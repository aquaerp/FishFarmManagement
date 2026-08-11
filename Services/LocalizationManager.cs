using System.Globalization;
using System.Resources;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace FishFarmManager.Services;

public static class LocalizationManager
{
    private static readonly ResourceManager Resources =
        new("FishFarmManager.Resources.Strings", typeof(LocalizationManager).Assembly);
    private static readonly ConditionalWeakTable<object, ResourceBinding> Bindings = new();
    private static readonly Lazy<IReadOnlyDictionary<string, string>> AutomaticResourceKeys =
        new(CreateAutomaticResourceKeys);

    public static CultureInfo CurrentCulture { get; private set; } =
        SupportedCultures.Create(SupportedCultures.Default);

    public static event EventHandler? CultureChanged;

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Resources.GetString(key, CurrentCulture) ?? $"[{key}]";
    }

    public static string Format(string key, params object?[] arguments) =>
        string.Format(CurrentCulture, Get(key), arguments);

    public static string GetUserMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return BusinessRuleError.TryGetCode(exception, out var code)
            ? Get($"Error.{code}")
            : Get("UnexpectedOperationFailure");
    }

    public static T Bind<T>(T target, string resourceKey) where T : class
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        Bindings.Remove(target);
        Bindings.Add(target, new ResourceBinding(resourceKey));
        ApplyBoundResource(target);
        return target;
    }

    public static void ApplyResources(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        ApplyBoundResource(root);

        if (root is DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
                ApplyBoundResource(column);
        }

        if (root is ToolStrip strip)
        {
            foreach (ToolStripItem item in strip.Items)
                ApplyToolStripItem(item);
        }

        foreach (Control child in root.Controls)
            ApplyResources(child);
    }

    private static void ApplyToolStripItem(ToolStripItem item)
    {
        ApplyBoundResource(item);
        if (item is ToolStripDropDownItem dropDown)
        {
            foreach (ToolStripItem child in dropDown.DropDownItems)
                ApplyToolStripItem(child);
        }
    }

    private static void ApplyBoundResource(object target)
    {
        if (!Bindings.TryGetValue(target, out var binding))
        {
            var currentText = target switch
            {
                Control control => control.Text,
                ToolStripItem item => item.Text,
                DataGridViewColumn column => column.HeaderText,
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(currentText)
                || !AutomaticResourceKeys.Value.TryGetValue(currentText, out var resourceKey))
            {
                return;
            }

            binding = new ResourceBinding(resourceKey);
            Bindings.Add(target, binding);
        }
        var value = Get(binding.ResourceKey);

        switch (target)
        {
            case Control control:
                if (string.IsNullOrWhiteSpace(control.AccessibleName)
                    || control.AccessibleName == binding.LastValue)
                {
                    control.AccessibleName = value;
                }
                control.Text = value;
                break;
            case ToolStripItem item:
                item.Text = value;
                item.AccessibleName = value;
                break;
            case DataGridViewColumn column:
                column.HeaderText = value;
                break;
            default:
                throw new ArgumentException(
                    $"Unsupported localization target: {target.GetType().FullName}", nameof(target));
        }

        binding.LastValue = value;
    }

    private static IReadOnlyDictionary<string, string> CreateAutomaticResourceKeys()
    {
        var resourceSet = Resources.GetResourceSet(
            SupportedCultures.Create(SupportedCultures.Arabic),
            createIfNotExists: true,
            tryParents: true);
        if (resourceSet == null) return new Dictionary<string, string>();

        return resourceSet.Cast<DictionaryEntry>()
            .Select(entry => new
            {
                Key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty,
                Value = Convert.ToString(entry.Value, CultureInfo.InvariantCulture) ?? string.Empty
            })
            .Where(entry => entry.Key.Length > 0 && entry.Value.Length > 0 && !entry.Value.Contains('{'))
            .GroupBy(entry => entry.Value, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().Key, StringComparer.Ordinal);
    }

    public static IReadOnlyCollection<string> GetAvailableKeys(string cultureName)
    {
        var resourceSet = Resources.GetResourceSet(
            SupportedCultures.Create(cultureName),
            createIfNotExists: true,
            tryParents: true);
        if (resourceSet == null) return Array.Empty<string>();

        return resourceSet.Cast<DictionaryEntry>()
            .Select(entry => Convert.ToString(entry.Key, CultureInfo.InvariantCulture))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Cast<string>()
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
    }

    public static void SetCulture(string? cultureName)
    {
        var culture = SupportedCultures.Create(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CurrentCulture = culture;
        CultureChanged?.Invoke(null, EventArgs.Empty);
    }

    private sealed class ResourceBinding(string resourceKey)
    {
        public string ResourceKey { get; } = resourceKey;
        public string? LastValue { get; set; }
    }
}
