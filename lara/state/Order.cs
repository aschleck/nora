using System;
using System.Collections.Immutable;

namespace nora.lara.state {
    
    public struct Order {

        public static Order MakeAbilityUpgrade(Vector heroOrigin, UInt16 abilityEntIndex) {
            return new Order() {
                OrderType = 0xb,
                BaseNpcHandle = abilityEntIndex,
            };
        }

        public static Order MakeMouseClick(ImmutableList<UInt16> selected, Vector point) {
            return new Order() {
                OrderType = 1,
                SelectedUnits = selected,
                PreparedOrderPoint = point
            };
        }

        public ImmutableList<UInt16> SelectedUnits { get; private set; } // 0x48
        public UInt16 PrimarySelected { get; private set; } // 0x58
        public Int16 OrderType { get; private set; } // 0x5c
        public Int16 EntityIndex1 { get; private set; } // 0x60
        public UInt16 BaseNpcHandle { get; private set; } // 0x64
        public Vector PreparedOrderPoint { get; private set; } // 0x68
        public bool QueueOrder { get; private set; } // 0x74
    }
}
