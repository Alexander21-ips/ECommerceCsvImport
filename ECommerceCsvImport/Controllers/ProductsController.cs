using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceCsvImport.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductRepository _repository;
        private readonly IProductImportService _importService;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(
            IProductRepository repository,
            IProductImportService importService,
            IWebHostEnvironment environment)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        [HttpGet]
        public IActionResult Index(string searchTerm = "")
        {
            var all = _repository.GetAll();

            // Доп. задание: поиск по названию
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                all = all.Where(p =>
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var viewModel = new ProductsIndexViewModel
            {
                Products = all,
                SearchTerm = searchTerm
            };
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View(new ImportViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Import(string filePath)
        {
            var viewModel = new ImportViewModel { FilePath = filePath ?? string.Empty };

            if (string.IsNullOrWhiteSpace(filePath))
            {
                ViewBag.Error = "Укажите путь к CSV-файлу";
                return View(viewModel);
            }

            var resolvedPath = ResolveFilePath(filePath);
            var result = _importService.ImportFromCsv(resolvedPath);
            viewModel.Result = result;

            return View(viewModel);
        }

        private string ResolveFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            if (filePath.StartsWith("/") || filePath.StartsWith("~"))
            {
                var relative = filePath.TrimStart('/', '~')
                                       .Replace('/', Path.DirectorySeparatorChar);
                return Path.Combine(_environment.WebRootPath, relative);
            }

            if (Path.IsPathRooted(filePath))
                return filePath;

            return Path.Combine(_environment.WebRootPath,
                                filePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}