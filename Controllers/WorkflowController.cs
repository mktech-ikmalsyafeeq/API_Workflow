using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

[ApiController]
[Route("api/workflow")]
public class WorkflowController : ControllerBase
{
    // GET api/workflow/health
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new[] { "value1", "value2" });
    }

    // POST api/workflow/trigger-docker-update?value=mes&branch=main
    [HttpPost("trigger-docker-update")]
    public async Task<IActionResult> TriggerDockerUpdate(
        [FromQuery] string value,
        [FromQuery] string branch)
    {
        try
        {
            await Task.Delay(1);

            if (value == "mes" && branch == "main")
            {
                string commands = @"
                    cd github_repo && \
                    cd mes-ui && \
                    git pull origin main && \
                    docker build -t mes-ui . && \
                    docker stop mes-ui || true && \
                    docker rm -f mes-ui || true && \
                    docker run -d -p 4200:80 --name mes-ui mes-ui
                ";

                string commands2 = @"
                    cd github_repo && \
                    cd mes-api && \
                    git pull origin main && \
                    docker build -t mes-api . && \
                    docker stop mes-api || true && \
                    docker rm -f mes-api || true && \
                    docker run -d -p 5000:5000 --name mes-api mes-api
                ";

                RunBash(commands);
                RunBash(commands2);

                return Ok("Docker update triggered successfully");
            }

            return BadRequest("Invalid parameter");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    private static void RunBash(string commands)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"" + commands.Replace("\"", "\\\"") + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        Console.WriteLine(stdout);

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine(stderr);
            throw new Exception($"Deployment failed, {stderr}");
        }
    }
}
