using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ssw2026TermProject.Data;
using ssw2026TermProject.Models;
using System.Security.Claims;

namespace ssw2026TermProject.Controllers
{

    [Authorize]
    public class UploadController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UploadController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            IFormFile imageFile,
            string? description,
            string? altText)
        {
            
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError(
                    "imageFile",
                    "Please select an image.");

                return View();
            }

            
            string[] allowedExtensions =
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            string extension = Path.GetExtension(
                imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "imageFile",
                    "Invalid image format.");

                return View();
            }

            
            const long maxFileSize = 5 * 1024 * 1024;

            if (imageFile.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "imageFile",
                    "Image cannot exceed 5 MB.");

                return View();
            }

           
            string? staffMemberId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(staffMemberId))
            {
                return Challenge();
            }

            
            string tempFolder = Path.Combine(
                _environment.WebRootPath, "tempImages");

            string approvedFolder = Path.Combine(
                _environment.WebRootPath, "approvedImages");

            Directory.CreateDirectory(tempFolder);
            Directory.CreateDirectory(approvedFolder);

            
            string baseFileName = Path.GetFileNameWithoutExtension(
                imageFile.FileName);

            
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                baseFileName = baseFileName.Replace(c.ToString(), "");
            }

            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                ModelState.AddModelError(
                    "imageFile",
                    "Invalid filename.");

                return View();
            }

           
            string fileName = baseFileName + extension;
            int counter = 1;

            while (
                System.IO.File.Exists(Path.Combine(tempFolder, fileName)) ||
                System.IO.File.Exists(Path.Combine(approvedFolder, fileName)))
            {
                fileName = baseFileName + counter + extension;
                counter++;
            }

            
            string tempFilePath = Path.Combine(tempFolder, fileName);

            using (var stream = new FileStream(
                tempFilePath, FileMode.CreateNew))
            {
                await imageFile.CopyToAsync(stream);
            }

            
            UploadedImage uploadedImage = new UploadedImage
            {
                FilePath = "approvedImages/" + fileName,
                Description = description,
                AltText = altText,
                UploadDateTime = DateTime.Now,
                StaffMemberId = staffMemberId,
                IsApproved = false
            };

            
            _context.UploadedImages.Add(uploadedImage);

            await _context.SaveChangesAsync();

            ViewBag.Message =
                "Image uploaded successfully. It is waiting for approval.";

            return View();
        }
    }
}

