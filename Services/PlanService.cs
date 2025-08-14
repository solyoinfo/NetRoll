using System.Text.Json;
using Microsoft.Extensions.Localization;
using NetRoll.Models;

namespace NetRoll.Services
{
    public class PlanService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PlanService> _logger;
        private readonly IStringLocalizer<PlanService> _loc;
        private List<PlanDefinition> _plans = new();
        private DateTime _loadedUtc;
        private readonly object _lock = new();
        private const string FileName = "plans.json";
        public PlanService(IWebHostEnvironment env, ILogger<PlanService> logger, IStringLocalizer<PlanService> loc)
        { _env = env; _logger = logger; _loc = loc; }

        private string ConfigPath => Path.Combine(_env.ContentRootPath, "App_Data");
        private string PlanFile => Path.Combine(ConfigPath, FileName);

        public IReadOnlyList<PlanDefinition> GetPlans()
        {
            EnsureLoaded();
            return _plans;
        }

        public PlanDefinition GetDefaultPlan()
        {
            EnsureLoaded();
            return _plans.FirstOrDefault() ?? new PlanDefinition { Name = "FREE", MaxFileCount = 50, MaxStorageBytes = 50 * 1024 * 1024, MaxProductCount = 20 };
        }

        public string GetDisplayName(string planName)
        {
            if (string.IsNullOrWhiteSpace(planName)) return "";
            var key = $"Plan_{planName.ToUpperInvariant()}";
            var loc = _loc[key];
            return string.IsNullOrEmpty(loc) ? planName : loc;
        }

        public void Reload()
        {
            lock (_lock)
            {
                _plans.Clear();
                _loadedUtc = DateTime.MinValue;
            }
            EnsureLoaded();
        }

        private void EnsureLoaded()
        {
            lock (_lock)
            {
                if (_plans.Count > 0 && (DateTime.UtcNow - _loadedUtc) < TimeSpan.FromMinutes(5)) return;
                try
                {
                    Directory.CreateDirectory(ConfigPath);
                    if (File.Exists(PlanFile))
                    {
                        var json = File.ReadAllText(PlanFile);
                        var list = JsonSerializer.Deserialize<List<PlanDefinition>>(json) ?? new List<PlanDefinition>();
                        if (list.Count > 0) _plans = list;
                    }
                    else
                    {
                        _plans = new List<PlanDefinition>
                        {
                            new PlanDefinition{ Name = "FREE", MaxFileCount=50, MaxStorageBytes=50*1024*1024, MaxProductCount=20},
                            new PlanDefinition{ Name = "PRO", MaxFileCount=1000, MaxStorageBytes=1024*1024*1024, MaxProductCount=1000}
                        };
                        File.WriteAllText(PlanFile, JsonSerializer.Serialize(_plans, new JsonSerializerOptions{WriteIndented=true}));
                    }
                    _loadedUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plans");
                    if (_plans.Count == 0)
                    {
                        _plans = new List<PlanDefinition>{ new PlanDefinition{ Name="FREE", MaxFileCount=50, MaxStorageBytes=50*1024*1024, MaxProductCount=20 } };
                    }
                }
            }
        }
    }
}
