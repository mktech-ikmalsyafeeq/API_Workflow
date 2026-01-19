using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API_Workflow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        // GET: api/<WorkflowController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }


        // POST api/<WorkflowController>
        [HttpPost]
        public async Task<IActionResult> TriggerDockerUpdate(string value, string branch)
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
                        docker build -t mes-ui mes-ui . && \
                        docker stop mes-ui && \
                        docker rm -f mes-ui && \
                        docker run -d -p 4200:80 --name mes-ui mes-ui
                    ";

                    string commands2 = @"
                        cd github_repo && \
                        cd mes-api && \
                        git pull origin main && \
                        docker build -t mes-api mes-api . && \
                        docker stop mes-api && \
                        docker rm -f mes-api && \
                        docker run -d -p 5000:5000 --name mes-api mes-api
                    ";

                    RunBash(commands);
                    RunBash(commands2);

                    return Ok();
                }

                return BadRequest("Invalid parameter");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        static void RunBash(string commands)
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
                throw new Exception("Deployment failed");
            }
        }
    }


}
