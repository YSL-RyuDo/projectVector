using System;
using System.Collections.Generic;

namespace ProjectVector.Core
{
    // A caller may release only its own hide request. A camera refresh must not
    // cancel a skill's request, nor may skill recovery cancel a death request.
    public sealed class EquipmentVisibilityState
    {
        private readonly HashSet<object> hiddenBy = new HashSet<object>();

        public bool IsVisible => hiddenBy.Count == 0;

        public bool SetVisibleFor(object owner, bool visible)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            bool previous = IsVisible;
            if (visible) hiddenBy.Remove(owner);
            else hiddenBy.Add(owner);
            return previous != IsVisible;
        }

        public void ResetForNewLife() => hiddenBy.Clear();
    }
}
