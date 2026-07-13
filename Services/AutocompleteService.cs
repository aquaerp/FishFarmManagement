using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة الإكمال الذكي الشاملة مع التخزين المؤقت والتعلم من الاستخدام
    /// Smart Autocomplete Service with Caching and Usage Pattern Learning
    /// </summary>
    public class AutocompleteService
    {
        private readonly FishFarmContext _context;
        
        // Cache للنتائج مع توقيت انتهاء الصلاحية
        private static Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
        
        // تتبع أنماط الاستخدام
        private static Dictionary<string, UsagePattern> _usagePatterns = new Dictionary<string, UsagePattern>();
        
        // مدة صلاحية الـ Cache (بالدقائق)
        private const int CacheExpirationMinutes = 15;
        
        // أقصى عدد للنتائج المعادة
        private const int MaxResults = 20;

        public AutocompleteService(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Core Search Methods

        /// <summary>
        /// البحث الموحد عن أي نوع من الكيانات
        /// </summary>
        public async Task<List<AutocompleteResult>> SearchAsync(
            string entityType, 
            string searchText, 
            string searchField = "Name",
            int? limitResults = null)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
                return new List<AutocompleteResult>();

            // إنشاء مفتاح Cache فريد
            string cacheKey = $"{entityType}_{searchField}_{searchText.ToLower()}";

            // التحقق من Cache أولاً
            if (TryGetFromCache(cacheKey, out var cachedResults))
                return cachedResults!;

            // البحث في قاعدة البيانات
            var results = entityType.ToLower() switch
            {
                "customer" or "customers" or "عملاء" => await SearchCustomersAsync(searchText, searchField),
                "supplier" or "suppliers" or "موردين" => await SearchSuppliersAsync(searchText, searchField),
                "inventoryitem" or "item" or "أصناف" => await SearchInventoryItemsAsync(searchText, searchField),
                "employee" or "employees" or "موظفين" => await SearchEmployeesAsync(searchText, searchField),
                "equipment" or "معدات" => await SearchEquipmentAsync(searchText, searchField),
                "pond" or "أحواض" => await SearchPondsAsync(searchText, searchField),
                _ => new List<AutocompleteResult>()
            };

            // تطبيق ترتيب حسب أنماط الاستخدام
            results = ApplyUsagePatternSorting(results, entityType);

            // تطبيق الحد الأقصى للنتائج
            int limit = limitResults ?? MaxResults;
            results = results.Take(limit).ToList();

            // حفظ في Cache
            AddToCache(cacheKey, results);

            return results;
        }

        /// <summary>
        /// البحث المتقدم مع شروط إضافية
        /// </summary>
        public async Task<List<AutocompleteResult>> SearchWithFilterAsync<T>(
            string searchText,
            Func<IQueryable<T>, IQueryable<T>> filter,
            Func<T, AutocompleteResult> selector) where T : class
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
                return new List<AutocompleteResult>();

            var query = _context.Set<T>().AsQueryable();
            
            if (filter != null)
                query = filter(query);

            var results = await query
                .Take(MaxResults)
                .ToListAsync();

            return results.Select(selector).ToList();
        }

        #endregion

        #region Entity-Specific Search Methods

        /// <summary>
        /// البحث عن العملاء
        /// </summary>
        private async Task<List<AutocompleteResult>> SearchCustomersAsync(string searchText, string searchField)
        {
            var query = _context.Customers
                .Where(c => c.Status == CustomerStatus.Active)
                .AsQueryable();

            // البحث في الحقل المحدد أو في جميع الحقول
            if (searchField == "Name" || searchField == "All")
            {
                query = query.Where(c => 
                    c.Name.Contains(searchText) ||
                    (c.Phone != null && c.Phone.Contains(searchText)) ||
                    (c.Email != null && c.Email.Contains(searchText)) ||
                    (c.TaxNumber != null && c.TaxNumber.Contains(searchText))
                );
            }
            else if (searchField == "Phone")
            {
                query = query.Where(c => c.Phone != null && c.Phone.Contains(searchText));
            }
            else if (searchField == "Email")
            {
                query = query.Where(c => c.Email != null && c.Email.Contains(searchText));
            }

            var customers = await query
                .OrderBy(c => c.Name)
                .Take(MaxResults)
                .ToListAsync();

            return customers.Select(c => new AutocompleteResult
            {
                Id = c.Id,
                DisplayText = c.Name,
                SecondaryText = FormatCustomerDetails(c),
                Value = c.Name,
                EntityType = "Customer",
                AdditionalData = new Dictionary<string, object>
                {
                    { "Id", c.Id },
                    { "Name", c.Name },
                    { "Phone", c.Phone ?? "" },
                    { "Email", c.Email ?? "" },
                    { "Address", c.Address ?? "" },
                    { "TaxNumber", c.TaxNumber ?? "" },
                    { "CreditLimit", c.CreditLimit },
                    { "CurrentBalance", c.CurrentBalance },
                    { "Type", c.Type.ToString() }
                }
            }).ToList();
        }

        /// <summary>
        /// البحث عن الموردين
        /// </summary>
        private async Task<List<AutocompleteResult>> SearchSuppliersAsync(string searchText, string searchField)
        {
            var query = _context.Suppliers
                .Where(s => s.Status == SupplierStatus.Active)
                .AsQueryable();

            if (searchField == "Name" || searchField == "All")
            {
                query = query.Where(s => 
                    s.Name.Contains(searchText) ||
                    (s.ContactPerson != null && s.ContactPerson.Contains(searchText)) ||
                    (s.Phone != null && s.Phone.Contains(searchText)) ||
                    (s.Email != null && s.Email.Contains(searchText))
                );
            }

            var suppliers = await query
                .OrderBy(s => s.Name)
                .Take(MaxResults)
                .ToListAsync();

            return suppliers.Select(s => new AutocompleteResult
            {
                Id = s.Id,
                DisplayText = s.Name,
                SecondaryText = FormatSupplierDetails(s),
                Value = s.Name,
                EntityType = "Supplier",
                AdditionalData = new Dictionary<string, object>
                {
                    { "Id", s.Id },
                    { "Name", s.Name },
                    { "ContactPerson", s.ContactPerson ?? "" },
                    { "Phone", s.Phone ?? "" },
                    { "Email", s.Email ?? "" },
                    { "Address", s.Address ?? "" },
                    { "City", s.City ?? "" },
                    { "TaxNumber", s.TaxNumber ?? "" },
                    { "Type", s.Type.ToString() }
                }
            }).ToList();
        }

        /// <summary>
        /// البحث عن أصناف المخزون
        /// </summary>
        private async Task<List<AutocompleteResult>> SearchInventoryItemsAsync(string searchText, string searchField)
        {
            var query = _context.InventoryItems
                .Where(i => i.IsActive)
                .AsQueryable();

            if (searchField == "Name" || searchField == "All")
            {
                query = query.Where(i => 
                    i.Name.Contains(searchText) ||
                    (i.Code != null && i.Code.Contains(searchText)) ||
                    (i.Barcode != null && i.Barcode.Contains(searchText)) ||
                    (i.Description != null && i.Description.Contains(searchText))
                );
            }
            else if (searchField == "Code")
            {
                query = query.Where(i => i.Code != null && i.Code.Contains(searchText));
            }
            else if (searchField == "Barcode")
            {
                query = query.Where(i => i.Barcode != null && i.Barcode.Contains(searchText));
            }

            var items = await query
                .OrderBy(i => i.Name)
                .Take(MaxResults)
                .ToListAsync();

            return items.Select(i => new AutocompleteResult
            {
                Id = i.Id,
                DisplayText = i.Name,
                SecondaryText = FormatInventoryItemDetails(i),
                Value = i.Name,
                EntityType = "InventoryItem",
                AdditionalData = new Dictionary<string, object>
                {
                    { "Id", i.Id },
                    { "Name", i.Name },
                    { "Code", i.Code ?? "" },
                    { "Category", i.Category.ToString() },
                    { "Unit", i.Unit },
                    { "CurrentStock", i.CurrentStock },
                    { "UnitCost", i.UnitCost },
                    { "Barcode", i.Barcode ?? "" },
                    { "Description", i.Description ?? "" }
                }
            }).ToList();
        }

        /// <summary>
        /// البحث عن الموظفين
        /// </summary>
        private async Task<List<AutocompleteResult>> SearchEmployeesAsync(string searchText, string searchField)
        {
            var query = _context.Employees
                .Where(e => e.Status == EmployeeStatus.Active)
                .AsQueryable();

            query = query.Where(e => 
                e.Name.Contains(searchText) ||
                (e.Phone != null && e.Phone.Contains(searchText))
            );

            var employees = await query
                .OrderBy(e => e.Name)
                .Take(MaxResults)
                .ToListAsync();

            return employees.Select(e => new AutocompleteResult
            {
                Id = e.Id,
                DisplayText = e.Name,
                SecondaryText = $"{e.Position} - {e.Department}",
                Value = e.Name,
                EntityType = "Employee",
                AdditionalData = new Dictionary<string, object>
                {
                    { "Id", e.Id },
                    { "Name", e.Name },
                    { "Position", e.Position },
                    { "Department", e.Department },
                    { "Phone", e.Phone ?? "" },
                    { "Email", e.Email ?? "" }
                }
            }).ToList();
        }

        /// <summary>
        /// البحث عن المعدات
        /// </summary>
        private async Task<List<AutocompleteResult>> SearchEquipmentAsync(string searchText, string searchField)
        {
            var query = _context.Equipment
                .Where(e => e.Status != null && (e.Status == "نشط" || e.Status == "Active"))
                .AsQueryable();

            query = query.Where(e => 
                e.Name.Contains(searchText) ||
                (e.SerialNumber != null && e.SerialNumber.Contains(searchText)) ||
                (e.Description != null && e.Description.Contains(searchText))
            );

            var equipment = await query
                .OrderBy(e => e.Name)
                .Take(MaxResults)
                .ToListAsync();

            return equipment.Select(e => new AutocompleteResult
            {
                Id = e.Id,
                DisplayText = e.Name,
                SecondaryText = e.SerialNumber ?? "",
                Value = e.Name,
                EntityType = "Equipment",
                AdditionalData = new Dictionary<string, object>
                {
                    { "Id", e.Id },
                    { "Name", e.Name },
                    { "SerialNumber", e.SerialNumber ?? "" },
                    { "Description", e.Description ?? "" }
                }
            }).ToList();
        }

        /// <summary>
        /// البحث عن الأحواض
        /// </summary>
        private async Task<List<AutocompleteResult>> SearchPondsAsync(string searchText, string searchField)
        {
            var query = _context.Ponds
                .Where(p => p.Status == PondStatus.Active)
                .AsQueryable();

            query = query.Where(p => 
                p.Name.Contains(searchText) ||
                (p.Description != null && p.Description.Contains(searchText))
            );

            var ponds = await query
                .OrderBy(p => p.Name)
                .Take(MaxResults)
                .ToListAsync();

            return ponds.Select(p => new AutocompleteResult
            {
                Id = p.Id,
                DisplayText = p.Name,
                SecondaryText = $"السعة: {p.Capacity} - المساحة: {p.Area}م²",
                Value = p.Name,
                EntityType = "Pond",
                AdditionalData = new Dictionary<string, object>
                {
                    { "Id", p.Id },
                    { "Name", p.Name },
                    { "Capacity", p.Capacity },
                    { "Area", p.Area },
                    { "Depth", p.Depth },
                    { "Description", p.Description ?? "" }
                }
            }).ToList();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// تنسيق تفاصيل العميل
        /// </summary>
        private string FormatCustomerDetails(Customer customer)
        {
            var details = new List<string>();
            
            if (!string.IsNullOrEmpty(customer.Phone))
                details.Add($"📞 {customer.Phone}");
            
            if (customer.Type != CustomerType.Individual)
                details.Add($"🏢 {GetCustomerTypeDisplay(customer.Type)}");
            
            if (customer.CurrentBalance != 0)
                details.Add($"💰 {customer.CurrentBalance:N2} ريال");

            return string.Join(" | ", details);
        }

        /// <summary>
        /// تنسيق تفاصيل المورد
        /// </summary>
        private string FormatSupplierDetails(Supplier supplier)
        {
            var details = new List<string>();
            
            if (!string.IsNullOrEmpty(supplier.ContactPerson))
                details.Add($"👤 {supplier.ContactPerson}");
            
            if (!string.IsNullOrEmpty(supplier.Phone))
                details.Add($"📞 {supplier.Phone}");
            
            if (!string.IsNullOrEmpty(supplier.City))
                details.Add($"📍 {supplier.City}");

            return string.Join(" | ", details);
        }

        /// <summary>
        /// تنسيق تفاصيل صنف المخزون
        /// </summary>
        private string FormatInventoryItemDetails(InventoryItem item)
        {
            var details = new List<string>();
            
            if (!string.IsNullOrEmpty(item.Code))
                details.Add($"🔖 {item.Code}");
            
            details.Add($"📦 {item.CurrentStock:N2} {item.Unit}");
            details.Add($"💵 {item.UnitCost:N2} ريال");

            return string.Join(" | ", details);
        }

        /// <summary>
        /// الحصول على عرض نص نوع العميل
        /// </summary>
        private string GetCustomerTypeDisplay(CustomerType type)
        {
            return type switch
            {
                CustomerType.Individual => "فردي",
                CustomerType.Business => "تجاري",
                CustomerType.Government => "حكومي",
                CustomerType.Institutional => "مؤسسي",
                CustomerType.Wholesale => "جملة",
                CustomerType.Restaurant => "مطعم",
                CustomerType.Hotel => "فندق",
                CustomerType.Supermarket => "سوبر ماركت",
                CustomerType.Exporter => "مصدر",
                _ => "أخرى"
            };
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// محاولة الحصول على النتائج من الـ Cache
        /// </summary>
        private bool TryGetFromCache(string key, out List<AutocompleteResult>? results)
        {
            results = null;

            if (_cache.ContainsKey(key))
            {
                var entry = _cache[key];
                
                // التحقق من صلاحية الـ Cache
                if (DateTime.Now - entry.Timestamp < TimeSpan.FromMinutes(CacheExpirationMinutes))
                {
                    results = entry.Results;
                    return true;
                }
                else
                {
                    // إزالة العنصر منتهي الصلاحية
                    _cache.Remove(key);
                }
            }

            return false;
        }

        /// <summary>
        /// إضافة النتائج إلى الـ Cache
        /// </summary>
        private void AddToCache(string key, List<AutocompleteResult> results)
        {
            _cache[key] = new CacheEntry
            {
                Results = results,
                Timestamp = DateTime.Now
            };

            // تنظيف الـ Cache القديم (الاحتفاظ بآخر 100 عنصر فقط)
            if (_cache.Count > 100)
            {
                var oldestKeys = _cache
                    .OrderBy(x => x.Value.Timestamp)
                    .Take(20)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var oldKey in oldestKeys)
                    _cache.Remove(oldKey);
            }
        }

        /// <summary>
        /// مسح الـ Cache للكيان المحدد
        /// </summary>
        public void ClearCache(string? entityType = null)
        {
            if (string.IsNullOrEmpty(entityType))
            {
                _cache.Clear();
            }
            else
            {
                var keysToRemove = _cache.Keys
                    .Where(k => k.StartsWith(entityType.ToLower()))
                    .ToList();

                foreach (var key in keysToRemove)
                    _cache.Remove(key);
            }
        }

        #endregion

        #region Usage Pattern Learning

        /// <summary>
        /// تسجيل استخدام عنصر معين
        /// </summary>
        public void RecordUsage(string entityType, int entityId, string displayText)
        {
            string key = $"{entityType}_{entityId}";

            if (_usagePatterns.ContainsKey(key))
            {
                _usagePatterns[key].UsageCount++;
                _usagePatterns[key].LastUsedAt = DateTime.Now;
            }
            else
            {
                _usagePatterns[key] = new UsagePattern
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    DisplayText = displayText,
                    UsageCount = 1,
                    LastUsedAt = DateTime.Now
                };
            }
        }

        /// <summary>
        /// تطبيق ترتيب حسب أنماط الاستخدام
        /// </summary>
        private List<AutocompleteResult> ApplyUsagePatternSorting(List<AutocompleteResult> results, string entityType)
        {
            return results
                .OrderByDescending(r =>
                {
                    string key = $"{entityType}_{r.Id}";
                    if (_usagePatterns.ContainsKey(key))
                    {
                        var pattern = _usagePatterns[key];
                        
                        // حساب نقاط الأولوية بناءً على:
                        // 1. عدد مرات الاستخدام
                        // 2. حداثة الاستخدام (آخر 7 أيام)
                        int usageScore = pattern.UsageCount;
                        int recencyScore = (DateTime.Now - pattern.LastUsedAt).Days <= 7 ? 100 : 0;
                        
                        return usageScore + recencyScore;
                    }
                    return 0;
                })
                .ThenBy(r => r.DisplayText)
                .ToList();
        }

        /// <summary>
        /// الحصول على العناصر الأكثر استخداماً
        /// </summary>
        public List<AutocompleteResult> GetMostUsed(string entityType, int count = 10)
        {
            var mostUsed = _usagePatterns
                .Where(p => p.Value.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Value.UsageCount)
                .ThenByDescending(p => p.Value.LastUsedAt)
                .Take(count)
                .Select(p => new AutocompleteResult
                {
                    Id = p.Value.EntityId,
                    DisplayText = p.Value.DisplayText,
                    SecondaryText = $"استخدم {p.Value.UsageCount} مرة",
                    Value = p.Value.DisplayText,
                    EntityType = p.Value.EntityType
                })
                .ToList();

            return mostUsed;
        }

        #endregion
    }

    #region Support Classes

    /// <summary>
    /// نتيجة الإكمال الذكي
    /// </summary>
    public class AutocompleteResult
    {
        public int Id { get; set; }
        public string DisplayText { get; set; } = string.Empty;
        public string SecondaryText { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        public override string ToString() => DisplayText;
    }

    /// <summary>
    /// عنصر الـ Cache
    /// </summary>
    internal class CacheEntry
    {
        public List<AutocompleteResult> Results { get; set; } = new List<AutocompleteResult>();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// نمط الاستخدام
    /// </summary>
    internal class UsagePattern
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string DisplayText { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public DateTime LastUsedAt { get; set; }
    }

    #endregion
}

