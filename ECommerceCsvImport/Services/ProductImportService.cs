using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services
{
    public class ProductImportService : IProductImportService
    {
        private readonly IProductRepository _repository;

        private static readonly string[] RequiredColumns = new[]
        {
            "sku", "name", "description", "price", "currency",
            "category_name", "stock", "image_url", "is_active",
            "isbn", "author", "publisher", "publication_year"
        };

        public ProductImportService(IProductRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }


        public ImportResult ImportFromCsv(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return ErrorResult(0, "Файл не найден");
            }

            if (!File.Exists(filePath))
            {
                return ErrorResult(0, "Файл не найден");
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                return ImportFromLines(lines);
            }
            catch (IOException)
            {
                return ErrorResult(0, "Ошибка чтения файла");
            }
            catch (UnauthorizedAccessException)
            {
                return ErrorResult(0, "Нет доступа к файлу");
            }
        }

        private static ImportResult ErrorResult(int rowNumber, string message)
        {
            var result = new ImportResult();
            result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = message });
            return result;
        }


        public ImportResult ImportFromLines(IEnumerable<string> lines)
        {
            var result = new ImportResult();
            var lineList = lines?.ToList() ?? new List<string>();

            if (lineList.Count == 0 || lineList.All(string.IsNullOrWhiteSpace))
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = 0,
                    Message = "Данные для импорта отсутствуют"
                });
                return result;
            }

            int headerIndex = -1;
            for (int i = 0; i < lineList.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(lineList[i]))
                {
                    headerIndex = i;
                    break;
                }
            }

            if (headerIndex == -1)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = 0,
                    Message = "Данные для импорта отсутствуют"
                });
                return result;
            }

            int headerRowNumber = headerIndex + 1;

            var headerFields = CsvLineParser.Parse(lineList[headerIndex]);
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerFields.Count; i++)
            {
                var name = headerFields[i].Trim();
                if (!string.IsNullOrEmpty(name) && !columnMap.ContainsKey(name))
                    columnMap[name] = i;
            }

            var missing = RequiredColumns.Where(c => !columnMap.ContainsKey(c)).ToList();
            if (missing.Count > 0)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = headerRowNumber,
                    Message = "Неверные заголовки CSV"
                });
                return result;
            }

            var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int expectedFieldCount = headerFields.Count;

            for (int i = headerIndex + 1; i < lineList.Count; i++)
            {
                var rawLine = lineList[i];
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                int rowNumber = i + 1;
                result.TotalRows++;

                var values = CsvLineParser.Parse(rawLine);

                if (values.Count != expectedFieldCount)
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = "Неверное количество полей в строке"
                    });
                    continue;
                }

                string GetValue(string columnName)
                {
                    if (columnMap.TryGetValue(columnName, out int idx) && idx < values.Count)
                        return values[idx].Trim();
                    return string.Empty;
                }

                var rowErrors = new List<ImportError>();

                string sku = GetValue("sku");
                if (string.IsNullOrWhiteSpace(sku))
                {
                    rowErrors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = "Поле sku не может быть пустым"
                    });
                }

                string name = GetValue("name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    rowErrors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = "Поле name не может быть пустым"
                    });
                }

                string priceStr = GetValue("price");
                decimal price = 0;
                bool priceValid = decimal.TryParse(priceStr,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out price) && price > 0;

                if (!priceValid)
                {
                    rowErrors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = "Поле price должно быть числом больше 0"
                    });
                }

                string stockStr = GetValue("stock");
                int stock = 0;
                bool stockValid = int.TryParse(stockStr, out stock) && stock >= 0;
                if (!stockValid)
                {
                    rowErrors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = "Остаток на складе должен быть целым числом не меньше 0"
                    });
                }

                string activeStr = GetValue("is_active");
                bool isActive = false;
                bool activeValid = TryParseBool(activeStr, out isActive);
                if (!activeValid)
                {
                    rowErrors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = "Поле is_active должно иметь значение true, false, 1 или 0"
                    });
                }

                string isbn = GetValue("isbn");
                string isbnDigits = isbn.Replace("-", "").Replace(" ", "");
                bool isbnValid = isbnDigits.Length == 10 || isbnDigits.Length == 13;
                if (!isbnValid)
                {
                    rowErrors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = "Поле isbn обязательно и должно содержать 10 или 13 символов"
                    });
                }

                string author = GetValue("author");
                if (string.IsNullOrWhiteSpace(author))
                {
                    rowErrors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = "Поле author обязательно"
                    });
                }

                int maxYear = DateTime.Now.Year + 1;
                string yearStr = GetValue("publication_year");
                int publicationYear = 0;
                bool yearValid = int.TryParse(yearStr, out publicationYear)
                                 && publicationYear >= 1900
                                 && publicationYear <= maxYear;
                if (!yearValid)
                {
                    rowErrors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = $"Год издания должен быть целым числом от 1900 до {maxYear}"
                    });
                }

                if (rowErrors.Count > 0)
                {
                    result.Errors.AddRange(rowErrors);
                    continue;
                }

                bool isDuplicate = _repository.GetBySku(sku) != null
                                   || importedSkus.Contains(sku);
                if (isDuplicate)
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = "Артикул товара уже существует"
                    });
                    continue;
                }

                var product = new Product
                {
                    Sku = sku.Trim(),
                    Name = name.Trim(),
                    Description = GetValue("description"),
                    Price = price,
                    Currency = NormalizeCurrency(GetValue("currency")),
                    CategoryName = NormalizeCategory(GetValue("category_name")),
                    StockQuantity = stock,
                    ImageUrl = GetValue("image_url"),
                    IsActive = isActive,
                    Isbn = isbn.Trim(),
                    Author = author.Trim(),
                    Publisher = GetValue("publisher"),
                    PublicationYear = publicationYear
                };

                _repository.Add(product);
                importedSkus.Add(sku.Trim());
                result.ImportedCount++;
            }

            if (result.TotalRows == 0 && result.Errors.Count == 0)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = 0,
                    Message = "Данные для импорта отсутствуют"
                });
            }

            return result;
        }


        private static bool TryParseBool(string value, out bool result)
        {
            result = false;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var v = value.Trim().ToLowerInvariant();
            if (v == "true" || v == "1") { result = true; return true; }
            if (v == "false" || v == "0") { result = false; return true; }
            return false;
        }

        private static string NormalizeCurrency(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "RUB";
            return value.Trim().ToUpperInvariant();
        }

        private static string NormalizeCategory(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Без категории";
            return value.Trim();
        }
    }
}