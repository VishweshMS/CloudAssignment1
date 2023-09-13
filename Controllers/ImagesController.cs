using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

using ImageSharingWithUpload.Models;
using Microsoft.Extensions.Logging;

namespace ImageSharingWithUpload.Controllers
{
    public class ImagesController : Controller
    {
        private readonly IWebHostEnvironment hostingEnvironment;

        private readonly ILogger logger;

        public ImagesController(IWebHostEnvironment environment, ILogger<ImagesController> logger)
        {
            hostingEnvironment = environment;
            this.logger = logger;
        }

        protected void MkDirectories()
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment), "Hosting Environment is not initialized.");
            }

            try
            {
                //var dataDir = Path.Combine(hostingEnvironment.WebRootPath, "data", "images");
                var dataDir = Path.Combine("D:\\A1 Cloud\\ImageSharingWithUpload\\ImageSharingWithUpload\\wwwroot", "data", "images");

                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                var infoDir = Path.Combine(hostingEnvironment.WebRootPath, "data", "info");
                if (!Directory.Exists(infoDir))
                {
                    Directory.CreateDirectory(infoDir);
                }
            }
            catch (Exception ex)
            {
                // You can log the exception here or handle it as required.
                throw new InvalidOperationException("Failed to create required directories.", ex);
            }
        }


        protected string imageDataFile(string id)
        {
            return Path.Combine(
               "D:\\A1 Cloud\\ImageSharingWithUpload\\ImageSharingWithUpload\\wwwroot", "data", "images", id + ".jpg");
        }

        protected string imageInfoFile(string id)
        {
            return Path.Combine(
               "D:\\A1 Cloud\\ImageSharingWithUpload\\ImageSharingWithUpload\\wwwroot", "data", "info", id + ".js");
        }


        protected void CheckAda()
        {
            var cookie = Request.Cookies["ADA"];
            logger.LogDebug("ADA cookie value: " + cookie);
            if (cookie != null && "true".Equals(cookie))
            {
                ViewBag.isADA = true;
            }
            else
            {
                ViewBag.isADA = false;
            }
        }

        [HttpGet]
        public IActionResult Upload()
        {
            CheckAda();
            ViewBag.Message = "";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(Image image, IFormFile imageFile)
        {
            CheckAda();

            if (ModelState.IsValid)
            {
                var username = Request.Cookies["Username"];
                if (username == null)
                {
                    return RedirectToAction("Register", "Account");
                }

                image.Username = username;

                if (imageFile != null && imageFile.Length > 0 && imageFile.Length < 5 * 1024 * 1024) // 5 MB limit
                {
                    if (!Regex.IsMatch(image.Id, "^[a-zA-Z0-9_]+$"))
                    {
                        ViewBag.Message = "Image ID should be alphanumeric with underscores!";
                        return View(image);
                    }

                    if (imageFile.ContentType.ToLower() != "image/jpeg")
                    {
                        ViewBag.Message = "Only JPEG images are allowed!";
                        return View(image);
                    }

                    mkDirectories();

                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    if (string.IsNullOrEmpty(image.Id))
                    {
                        ViewBag.Message = "Image ID is not provided!";
                        return View(image);
                    }

                    using var fileStream = new FileStream(imageDataFile(image.Id), FileMode.Create);
                    await imageFile.CopyToAsync(fileStream);

                    var jsonData = JsonSerializer.Serialize(image, jsonOptions);
                    await System.IO.File.WriteAllTextAsync(imageInfoFile(image.Id), jsonData);

                    return View("Details", image);
                }
                else
                {
                    ViewBag.Message = "No image file specified or the file is too large!";
                    return View(image);
                }
            }
            else
            {
                ViewBag.Message = "Please correct the errors in the form!";
                return View(image);
            }
        }

        // TODO
        [HttpGet]
        public IActionResult Query()
        {
            CheckAda();
            ViewBag.Message = "";
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Query(string imageId)
        {
            CheckAda();
            if (string.IsNullOrEmpty(imageId))
            {
                ViewBag.Message = "Please provide an image ID.";
                return View();
            }

            // Redirect to the Details action to show the image details
            return RedirectToAction("Details", new { id = imageId });
        }

        // TODO
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            CheckAda();

            var username = Request.Cookies["Username"];
            if (username == null)
            {
                return RedirectToAction("Register", "Account");
            }

            if (string.IsNullOrEmpty(id))
            {
                ViewBag.Message = "Image ID is not provided!";
                return View("Query");
            }
            String fileName = imageInfoFile(id);
            if (System.IO.File.Exists(fileName))
            {
                String jsonData = await System.IO.File.ReadAllTextAsync(fileName);
                Image imageInfo = JsonSerializer.Deserialize<Image>(jsonData);

                return View(imageInfo);
            }
            else
            {
                ViewBag.Message = "Image with identifier " + id + " not found";
                ViewBag.Id = id;

                return View("Query");
            }
        }
    }
}