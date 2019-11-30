using System;
using System.Collections.Generic;

namespace Relua.Script {
    public partial class ReluaRuntime {
        private const int REF_CHECKPOINT = 50;

        public Dictionary<int, object> RefRegistry = new Dictionary<int, object>();
        private int RefCount = 0;
        private int LastNewHole = -1;

        private int? TryFindHole() {
            for (var i = 0; i < RefCount; i++) {
                if (RefRegistry[i] == null) return i;
            }
            return null;
        }

        public int CreateReference(object obj) {
            if (RefCount % 20 == 0) {
                var hole = TryFindHole();
                if (hole != null) {
                    RefRegistry[hole.Value] = obj;
                    return hole.Value;
                }
            }

            if (LastNewHole > -1) {
                var idx = LastNewHole;
                RefRegistry[LastNewHole] = obj;
                LastNewHole = -1;
                return idx;
            }

            RefRegistry[RefCount] = obj;
            RefCount += 1;

            return RefCount - 1;
        }

        public object ResolveReference(int id) {
            object obj = null;
            RefRegistry.TryGetValue(id, out obj);
            return obj;
        }

        public void RemoveReference(int id) {
            RefRegistry.Remove(id);
            LastNewHole = id;
        }
    }
}
