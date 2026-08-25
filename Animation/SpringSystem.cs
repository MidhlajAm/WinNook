using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Project_Nook.Animation
{
    /// <summary>
    /// Drives a set of springs off the display-synced render clock, using the real frame delta.
    /// The hook is detached as soon as every spring has settled, so an idle notch costs nothing.
    /// </summary>
    public sealed class SpringSystem
    {
        private readonly List<SpringDouble> _springs = new();
        private bool _running;
        private TimeSpan _lastRenderTime = TimeSpan.MinValue;

        public void Add(SpringDouble spring) => _springs.Add(spring);

        /// <summary>Start ticking. Safe to call on every retarget.</summary>
        public void Start()
        {
            if (_running) return;
            _running = true;
            _lastRenderTime = TimeSpan.MinValue;
            CompositionTarget.Rendering += OnRendering;
        }

        private void Stop()
        {
            if (!_running) return;
            _running = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            // RenderingEventArgs carries the frame time, and the same frame can raise this more than once.
            if (e is not RenderingEventArgs args) return;
            if (args.RenderingTime == _lastRenderTime) return;

            double dt = _lastRenderTime == TimeSpan.MinValue
                ? 1.0 / 60.0
                : (args.RenderingTime - _lastRenderTime).TotalSeconds;
            _lastRenderTime = args.RenderingTime;

            bool anyMoving = false;
            foreach (var spring in _springs)
            {
                if (spring.Tick(dt)) anyMoving = true;
            }

            if (!anyMoving) Stop();
        }
    }
}
