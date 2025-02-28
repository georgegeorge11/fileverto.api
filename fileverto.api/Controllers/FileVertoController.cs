using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace fileverto.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileVertoController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private static readonly Dictionary<string, string> _convertedFiles = new();

        public FileVertoController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFiles([FromForm] IFormFileCollection files, [FromForm] string source, [FromForm] string destination)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files received.");

            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var convertedFiles = new List<object>();

            foreach (var file in files)
            {
                var originalFilePath = Path.Combine(uploadFolder, file.FileName);

                // Save the original file
                using (var stream = new FileStream(originalFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Convert File (Simulated)
                var convertedFilePath = originalFilePath.Replace($".{source}", $".{destination}");
                System.IO.File.Copy(originalFilePath, convertedFilePath, true);

                // Store the converted file for later download
                var convertedFileName = Path.GetFileName(convertedFilePath);
                _convertedFiles[convertedFileName] = convertedFilePath;

                convertedFiles.Add(new { Name = convertedFileName, Url = $"/api/files/download/{convertedFileName}" });
            }

            return Ok(convertedFiles);
        }
        
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (!_convertedFiles.TryGetValue(fileName, out var filePath) || !System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileExtension = Path.GetExtension(fileName).TrimStart('.');
            var mimeType = GetMimeType(fileExtension);

            return File(fileBytes, mimeType, fileName);
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                "pdf" => "application/pdf",
                "txt" => "text/plain",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "jpg" => "image/jpeg",
                "png" => "image/png",
                "json" => "application/json",
                _ => "application/octet-stream",
            };
        }
    }
}
