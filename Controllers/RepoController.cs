using Microsoft.AspNetCore.Mvc;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TotsChallenge.Models;


namespace TotsChallenge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RepoController : ControllerBase
    {
        private GitHubClient client;

        [HttpGet]
        private void GetAuth()
        {
            try
            {
                //Set the personal access token from GitHub page to get user's credential
                string token = Encoding.UTF8.GetString(Convert.FromBase64String(Environment.GetEnvironmentVariable("PERSONAL_ACCESS_TOKEN", EnvironmentVariableTarget.Process)));
                Credentials credentials = new Credentials(token);
                client = new GitHubClient(new ProductHeaderValue(Environment.GetEnvironmentVariable("USER_GITHUB", EnvironmentVariableTarget.Process))) { Credentials = credentials };
            }
            catch (Exception e)
            {
                Console.WriteLine("[TotsChallenge.Controllers.Repo.GetAuth.Error]: " + e.Message);
            }
        }

        /// <summary>
        /// Create a new repository. The only parameter is the repository name. The characters limit is until 30. It have special characters will be delete.
        /// </summary>
        [HttpGet("/CreateRepo")]
        public async Task<IActionResult> CreateRepo([FromQuery] Models.Repository repository)
        {
            try
            {
                string newNameRepo = RemoveSpecialCharacters(repository.repo);

                if (newNameRepo.Length > 30)
                {
                    return BadRequest("The name of the new repo add is large, try to use another. The limit is 30 characters.");
                }

                var newRepo = new NewRepository(repository.repo)
                {
                    AutoInit = true,
                    Description = "This is an new repository from source code with number: " + (DateTime.UnixEpoch.Ticks / 1000000000),
                    Private = false,
                    IsTemplate = true
                };

                this.GetAuth();

                var repositoryResponse = await client.Repository.Create(newRepo);

                return Ok("The repository was created successfully.");
            }
            catch (Exception e)
            {
                return BadRequest("[TotsChallenge.Controllers.Repo.CreateRepo.Error]: " + e.Message);
            }
        }

        /// <summary>
        /// Delete an repository. The only parameter is the new repository name. Only can be delete the repository is template.
        /// </summary>
        [HttpPost("/DeleteRepo")]
        public async Task<IActionResult> DeleteRepo([FromQuery] Models.Repository repository)
        {
            try
            {
                this.GetAuth();

                var repositoryList = await client.Repository.GetAllForCurrent();

                foreach (var repo in repositoryList)
                {
                    if (repo.Name.Equals(repository.repo) && repo.IsTemplate == true)
                    {
                        //Only can be delete the repository when its attribute "template" is true
                        await client.Repository.Delete(repo.Id);
                        return Ok("The repository was deleted successfully.");
                    }
                }

                return BadRequest("The repository don't exist.");
            }
            catch (Exception e)
            {
                return BadRequest("[TotsChallenge.Controllers.Repo.DeleteRepo.Error]: " + e.Message);
            }
        }

        /// <summary>
        /// Get all the repository from user's account.
        /// </summary>
        [HttpGet("/ListRepo")]
        public async Task<IActionResult> ListRepo()
        {
            try
            {
                this.GetAuth();

                var repositoryList = await client.Repository.GetAllForCurrent();

                List<object> list = new List<object>();
                List<KeyValuePair<string, string>> item = null;

                foreach (var repo in repositoryList)
                {
                    item = new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("Id", repo.Id.ToString()),
                        new KeyValuePair<string, string>("Name", repo.Name),
                        new KeyValuePair<string, string>("Description", (string.IsNullOrEmpty(repo.Description) ? null : repo.Description))
                    };

                    list.Add(item);
                }

                return Ok(list);
            }
            catch (Exception e)
            {
                return BadRequest("[TotsChallenge.Controllers.Repo.ListRepo.Error]: " + e.Message);
            }
        }

        /// <summary>
        /// Add an new file at the repository.
        /// </summary>
        [HttpPost("/AddFile")]
        public async Task<IActionResult> AddNewFileToRepo([FromQuery] Models.Repository repository, [FromQuery] File file)
        {
            try
            {
                this.GetAuth();

                string newNameFile = RemoveSpecialCharacters(file.name);

                if (newNameFile.Length > 30)
                {
                    return BadRequest("The name of the new file add is large, try to use another. The limit is 30 characters.");
                }

                //Is necesary make control check about repo name because the user can write anything
                var repositoryList = await client.Repository.GetAllForCurrent();

                foreach (var repoItem in repositoryList)
                {
                    if (repoItem.Name.Equals(repository.repo))
                    {
                        //Now, is necesary check also if the new name file don't exist inside the repo
                        int fileExist = await this.SearchFile(repository.repo, file.name);

                        if (fileExist.Equals(0))
                        {
                            //Finally, add the new file
                            var sb = new StringBuilder("---");
                            sb.AppendLine();
                            sb.AppendLine($"date: \"" + DateTime.Now + "\"");
                            sb.AppendLine($"title: \"New file was added\"");
                            sb.AppendLine("---");
                            sb.AppendLine();

                            var (owner, repoName, filePath, branch) = (Environment.GetEnvironmentVariable("USER_GITHUB", EnvironmentVariableTarget.Process), repository.repo,
                                    "./" + newNameFile + "", "main");

                            await client.Repository.Content.CreateFile(
                                 owner, repoName, filePath,
                                 new CreateFileRequest($"First commit for {filePath}", sb.ToString(), branch));

                            return Ok("The file was added to repository successfully");
                        }

                        return BadRequest("Cannot add a new file with that name file because there another file was used.");

                    }
                }

                return BadRequest("The repository don't exist.");
            }
            catch (Exception e)
            {
                return BadRequest("[TotsChallenge.Controllers.Repo.AddNewFileToRepo.Error]: " + e.Message);
            }
        }

        private async Task<int> SearchFile(string repository, string name)
        {
            try
            {
                var result = await client.Repository.Content.GetAllContents(Environment.GetEnvironmentVariable("USER_GITHUB", EnvironmentVariableTarget.Process), repository, "./");
                return result.Where(files => files.Name.Equals(name)).Count();
            }
            catch (Exception e)
            {
                Console.WriteLine("[TotsChallenge.Controllers.Repo.SearchFile.Error]: " + e.Message);
                return -1;
            }
        }

        //This function only was used to control the characters's name file
        private string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^.a-zA-Z0-9_]+", "", RegexOptions.Compiled);
        }
    }
}
