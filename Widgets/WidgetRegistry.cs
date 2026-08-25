using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

namespace Project_Nook.Widgets
{
    /// <summary>One widget's saved state. Shape of the JSON stored under the "Widgets" setting key.</summary>
    public sealed class WidgetPreference
    {
        public string Id { get; set; }
        public bool Enabled { get; set; }
        public int Order { get; set; }
    }

    /// <summary>
    /// The single source of truth for which widgets exist, which are on, and what order they sit in.
    /// The dashboard renders from it and the Control Center's Activity tab edits it.
    ///
    /// Preferences persist as one JSON blob in settings.json, the same way the Dive In app list does
    /// (see ConfigurationService.GetDiveInApps).
    /// </summary>
    public partial class WidgetRegistry : ObservableObject
    {
        private const string SettingsKey = "Widgets";

        private readonly IConfigurationService _config;
        private readonly List<IWidgetDescriptor> _descriptors;
        private bool _isLoading;

        public ObservableCollection<WidgetSlot> Slots { get; } = new();

        /// <summary>Raised when a widget is toggled or reordered, so views can rebuild their lists.</summary>
        public event EventHandler LayoutChanged;

        public WidgetRegistry(IEnumerable<IWidgetDescriptor> descriptors, IConfigurationService config)
        {
            _config = config;
            _descriptors = descriptors.ToList();
            Load();
        }

        private void Load()
        {
            _isLoading = true;

            var saved = ReadPreferences();
            var byId = saved.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

            // Descriptors the user has seen before keep their saved order; anything new lands at the end.
            int nextOrder = saved.Count == 0 ? 0 : saved.Max(p => p.Order) + 1;

            var slots = new List<WidgetSlot>();
            foreach (var descriptor in _descriptors)
            {
                if (byId.TryGetValue(descriptor.Id, out var pref))
                {
                    slots.Add(new WidgetSlot(descriptor, pref.Enabled, pref.Order));
                }
                else
                {
                    slots.Add(new WidgetSlot(descriptor, descriptor.EnabledByDefault, nextOrder++));
                }
            }

            Slots.Clear();
            foreach (var slot in slots.OrderBy(s => s.Order))
            {
                slot.PropertyChanged += Slot_PropertyChanged;
                Slots.Add(slot);
            }

            Normalize();
            _isLoading = false;

            // A first run, or an upgrade from the era of per-widget booleans, needs the file written once.
            if (saved.Count == 0) Save();
        }

        private List<WidgetPreference> ReadPreferences()
        {
            var json = _config.GetSetting(SettingsKey);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<WidgetPreference>>(json);
                    if (parsed != null)
                        return parsed.Where(p => !string.IsNullOrEmpty(p.Id)).ToList();
                }
                catch { }
            }

            return MigrateLegacyPreferences();
        }

        /// <summary>
        /// Before the registry, Music and Calendar each had their own boolean in settings.json. Carry those
        /// choices forward so an existing user does not have widgets silently switch back on.
        /// </summary>
        private List<WidgetPreference> MigrateLegacyPreferences()
        {
            var migrated = new List<WidgetPreference>();

            void Carry(string legacyKey, string id, int order)
            {
                if (!_config.HasSetting(legacyKey)) return;
                if (!bool.TryParse(_config.GetSetting(legacyKey), out var enabled)) return;
                migrated.Add(new WidgetPreference { Id = id, Enabled = enabled, Order = order });
            }

            Carry("IsMusicWidgetEnabled", WidgetIds.Music, 0);
            Carry("IsCalendarWidgetEnabled", WidgetIds.Calendar, 1);

            return migrated;
        }

        private void Slot_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isLoading) return;
            if (e.PropertyName != nameof(WidgetSlot.IsEnabled) && e.PropertyName != nameof(WidgetSlot.Order))
                return;

            Save();
            RaiseLayoutChanged();
        }

        /// <summary>Collapses Order into a gapless 0..n-1 sequence matching the collection order.</summary>
        private void Normalize()
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                Slots[i].Order = i;
            }
        }

        private void Save()
        {
            var prefs = Slots
                .Select(s => new WidgetPreference { Id = s.Id, Enabled = s.IsEnabled, Order = s.Order })
                .ToList();

            _config.SetSetting(SettingsKey, JsonSerializer.Serialize(prefs));
        }

        private void RaiseLayoutChanged() => LayoutChanged?.Invoke(this, EventArgs.Empty);

        public WidgetSlot Find(string id) =>
            Slots.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

        public bool IsEnabled(string id) => Find(id)?.IsEnabled ?? false;

        /// <summary>Enabled widgets in user order, wide and tile alike. What the dashboard renders.</summary>
        public IEnumerable<WidgetSlot> Visible() =>
            Slots.Where(s => s.IsEnabled).OrderBy(s => s.Order);

        [RelayCommand]
        private void MoveUp(WidgetSlot slot) => Move(slot, -1);

        [RelayCommand]
        private void MoveDown(WidgetSlot slot) => Move(slot, 1);

        private void Move(WidgetSlot slot, int delta)
        {
            if (slot == null) return;

            int index = Slots.IndexOf(slot);
            int target = index + delta;
            if (index < 0 || target < 0 || target >= Slots.Count) return;

            _isLoading = true;
            Slots.Move(index, target);
            Normalize();
            _isLoading = false;

            Save();
            RaiseLayoutChanged();
        }

        public void RestoreDefaults()
        {
            _isLoading = true;

            var ordered = _descriptors.Select((d, i) => new { d, i }).ToList();
            foreach (var existing in Slots) existing.PropertyChanged -= Slot_PropertyChanged;
            Slots.Clear();
            foreach (var entry in ordered)
            {
                var slot = new WidgetSlot(entry.d, entry.d.EnabledByDefault, entry.i);
                slot.PropertyChanged += Slot_PropertyChanged;
                Slots.Add(slot);
            }

            _isLoading = false;

            Save();
            RaiseLayoutChanged();
        }
    }
}
