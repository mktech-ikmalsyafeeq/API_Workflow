using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Xml.Linq;

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
                // Full paths to commands
                string gitPath = "/usr/bin/git";
                string dockerPath = "/usr/bin/docker";

                // Command for mes-ui
                string commandsUI = $@"
            set -e
            if [ -d /home/dev/github_repo/mes-ui ]; then
                cd /home/dev/github_repo/mes-ui
                {gitPath} pull origin main
                {dockerPath} build -t mes-ui .
                {dockerPath} stop mes-ui
                {dockerPath} rm -f mes-ui
                {dockerPath} run -d -p 4200:80 --name mes-ui mes-ui
            else
                echo 'Directory /home/dev/github_repo/mes-ui does not exist'
                exit 1
            fi
        ";

                // Command for mes-api
                string commandsAPI = $@"
            set -e
            if [ -d /home/dev/github_repo/mes-api ]; then
                cd /home/dev/github_repo/mes-api
                {gitPath} pull origin main
                {dockerPath} build -t mes-api .
                {dockerPath} stop mes-api
                {dockerPath} rm -f mes-api
                {dockerPath} run -d -p 5000:5000 --name mes-api mes-api
            else
                echo 'Directory /home/dev/github_repo/mes-api does not exist'
                exit 1
            fi
        ";

                // Run mes-ui
                RunBash(commandsUI, "mes-ui");

                // Run mes-api
                RunBash(commandsAPI, "mes-api");

                return Ok("Docker update triggered successfully");
            }

            return BadRequest("Invalid parameter");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    private static void RunBash(string commands, string name)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{commands}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Console.WriteLine($"--- {name} OUTPUT ---");
        Console.WriteLine(output);
        if (!string.IsNullOrWhiteSpace(error))
        {
            throw new Exception($"Deployment failed, The error is {error}");
        }

    }
}
