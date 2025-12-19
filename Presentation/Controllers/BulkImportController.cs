using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Factory;
using Presentation.Models;

namespace Presentation.Controllers
{
    [AllowAnonymous]
    public class BulkImportController : Controller
    {
        private const string SESSION_KEY = "IMPORT_JSON";
        private static readonly byte[] DefaultImageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAukB9YUmRS0AAAAASUVORK5CYII=");

        private readonly IItemsRepository _repo;
        private readonly ImportItemFactory _factory;
        private readonly IWebHostEnvironment _environment;

        public BulkImportController(IItemsRepository repo, ImportItemFactory factory, IWebHostEnvironment environment)
        {
            _repo = repo;
            _factory = factory;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index() => View(new BulkImportViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(IFormFile? jsonFile, string? json)
        {
            var model = new BulkImportViewModel();
            var content = await ReadJson(jsonFile, json);
            if (string.IsNullOrWhiteSpace(content))
            {
                model.Error = "Please provide a JSON file or paste JSON content.";
                return View("Index", model);
            }

            try
            {
                var items = _factory.BuildList(content);
                HttpContext.Session.SetString(SESSION_KEY, content);
                model.Items = items;
                model.JsonInput = content;
            }
            catch (Exception ex)
            {
                model.Error = $"Could not parse JSON: {ex.Message}";
            }

            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Commit()
        {
            var json = HttpContext.Session.GetString(SESSION_KEY);
            if (string.IsNullOrWhiteSpace(json))
                return RedirectToAction("Index");

            var items = _factory.BuildList(json);
            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads");
            foreach (var menuItem in items.OfType<MenuItem>())
            {
                var itemFolder = Path.Combine(uploadsRoot, menuItem.Id);
                if (Directory.Exists(itemFolder))
                {
                    var firstImage = Directory
                        .EnumerateFiles(itemFolder)
                        .FirstOrDefault();
                    if (firstImage != null)
                    {
                        menuItem.ImagePath = ToWebPath(firstImage, _environment.WebRootPath);
                    }
                }
            }

            foreach (var item in items)
            {
                _repo.Add(item);
            }

            HttpContext.Session.Remove(SESSION_KEY);
            return View("Index", new BulkImportViewModel
            {
                Message = $"{items.Count} items committed to repository."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImages(IFormFile? imagesZip)
        {
            var model = BuildModelFromSession();
            if (imagesZip == null || imagesZip.Length == 0)
            {
                model.Error = "Please upload a .zip file containing item folders.";
                return View("Index", model);
            }

            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsRoot);

            using var archive = new ZipArchive(imagesZip.OpenReadStream());
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Name)) continue;

                var destinationPath = Path.Combine(uploadsRoot, entry.FullName);
                var fullPath = Path.GetFullPath(destinationPath);
                if (!fullPath.StartsWith(Path.GetFullPath(uploadsRoot)))
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await using var entryStream = entry.Open();
                await using var outputStream = System.IO.File.Create(fullPath);
                await entryStream.CopyToAsync(outputStream);
            }

            model.Message = "Images uploaded. You can now commit the import.";
            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadImageTemplate()
        {
            var json = HttpContext.Session.GetString(SESSION_KEY);
            if (string.IsNullOrWhiteSpace(json))
                return RedirectToAction("Index");

            var menuItems = _factory.BuildList(json).OfType<MenuItem>().ToList();
            if (menuItems.Count == 0)
                return RedirectToAction("Index");

            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (var item in menuItems)
                {
                    var entry = archive.CreateEntry($"{item.Id}/default.jpg");
                    await using var entryStream = entry.Open();
                    entryStream.Write(DefaultImageBytes, 0, DefaultImageBytes.Length);
                }
            }

            stream.Position = 0;
            return File(stream.ToArray(), "application/zip", "images-template.zip");
        }

        private async Task<string?> ReadJson(IFormFile? jsonFile, string? jsonText)
        {
            if (jsonFile != null && jsonFile.Length > 0)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                return await reader.ReadToEndAsync();
            }

            return jsonText;
        }

        private BulkImportViewModel BuildModelFromSession()
        {
            var model = new BulkImportViewModel();
            var json = HttpContext.Session.GetString(SESSION_KEY);
            if (!string.IsNullOrWhiteSpace(json))
            {
                model.JsonInput = json;
                model.Items = _factory.BuildList(json);
            }

            return model;
        }

        private static string ToWebPath(string fullPath, string webRoot)
        {
            var normalizedWebRoot = Path.GetFullPath(webRoot);
            var normalizedFullPath = Path.GetFullPath(fullPath);
            if (normalizedFullPath.StartsWith(normalizedWebRoot))
            {
                var relative = normalizedFullPath.Substring(normalizedWebRoot.Length).Replace("\\", "/");
                if (!relative.StartsWith("/")) relative = "/" + relative;
                return relative;
            }

            return fullPath;
        }
    }
}
