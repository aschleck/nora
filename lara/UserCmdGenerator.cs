using log4net;
using nora.lara.net;
using nora.lara.state;
using nora.protos;
using System.Collections.Generic;

namespace nora.lara {

    public class UserCmdGenerator : Commander {

        private static ILog log = LogManager.GetLogger(typeof(UserCmdGenerator));

        private readonly Client client;
        private readonly Connection connection;
        private readonly List<UserCmd> backup;
        private readonly List<UserCmd> next;
        private uint commandNumber;
        private short orderNumber;
        private Order? latestOrder;

        public UserCmdGenerator(Client client, Connection connection) {
            this.client = client;
            this.connection = connection;

            this.backup = new List<UserCmd>();
            this.next = new List<UserCmd>();

            this.commandNumber = 0;
            this.orderNumber = 0;
        }

        public void Tick() {
            next.Add(MakeUserCmd());
            if (next.Count > 1) {
                SendUserCmds();
            }
        }

        public void Reset() {
            commandNumber = 40;

            backup.Clear();
            backup.Add(new UserCmd());
            backup.Add(new UserCmd());

            next.Clear();
        }

        public void Submit(Order order) {
            ++orderNumber;
            latestOrder = order;
        }

        private UserCmd MakeUserCmd() {
            var thing = new UserCmd();
            thing.Command = commandNumber++;
            thing.Tick = client.ClientTick++; // TODO: This might be off by one.
            thing.ViewAngles = new Vector(60, 90, 0);
            thing.CrosshairTrace = new Vector(5548.375f, 3522.188f, 256);
            thing.QueryEntIndex = 1390;
            thing.MouseDx = 73;
            thing.MouseDy = 110;
            thing.OrderId = orderNumber;
            thing.Order = latestOrder;
            thing.CameraX = 6445;
            thing.CameraY = 2670;
            thing.ShopModeSomething = 15;
            thing.HudStatsDropdownCategoryIndex = 15;
            thing.HudStatsDropdownSortMethod = 2;
            return thing;
        }

        private void SendUserCmds() {
            var last = new UserCmd();

            using (var stream = Bitstream.Create()) {
                var move = new CCLCMsg_Move();

                move.num_backup_commands = (uint) backup.Count;
                foreach (var cmd in backup) {
                    WriteUserCmd(cmd, last, stream);
                    last = cmd;
                }

                move.num_new_commands = (uint) next.Count;
                foreach (var cmd in next) {
                    WriteUserCmd(cmd, last, stream);
                    last = cmd;
                }

                backup.Clear();
                backup.AddRange(next);
                next.Clear();

                move.data = stream.ToBytes();

                var tick = new CNETMsg_Tick();
                tick.tick = client.ServerTick;

                connection.SendUnreliably(
                    Connection.ConvertProtoToMessage<CCLCMsg_Move>((uint) CLC_Messages.clc_Move, move),
                    Connection.ConvertProtoToMessage<CNETMsg_Tick>((uint) NET_Messages.net_Tick, tick));
            }
        }

        private void WriteUserCmd(UserCmd cmd, UserCmd last, Bitstream stream) {
            if (cmd.Command != last.Command + 1) {
                stream.WriteBool(true);
                stream.WriteUInt32(cmd.Command);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.Tick != last.Tick + 1) {
                stream.WriteBool(true);
                stream.WriteUInt32(cmd.Tick);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.ViewAngles.X != last.ViewAngles.X) {
                stream.WriteBool(true);
                stream.WriteFloat(cmd.ViewAngles.X);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.ViewAngles.X != last.ViewAngles.X) {
                stream.WriteBool(true);
                stream.WriteFloat(cmd.ViewAngles.X);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.ViewAngles.Z != last.ViewAngles.Z) {
                stream.WriteBool(true);
                stream.WriteFloat(cmd.ViewAngles.Z);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.ForwardMove != last.ForwardMove) {
                stream.WriteBool(true);
                stream.WriteFloat(cmd.ForwardMove);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.SideMove != last.SideMove) {
                stream.WriteBool(true);
                stream.WriteFloat(cmd.SideMove);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.UpMove != last.UpMove) {
                stream.WriteBool(true);
                stream.WriteFloat(cmd.UpMove);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.Buttons != last.Buttons) {
                stream.WriteBool(true);
                stream.WriteUInt32(cmd.Buttons);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.Impulse != last.Impulse) {
                stream.WriteBool(true);
                stream.WriteByte(cmd.Impulse);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.CrosshairTrace != last.CrosshairTrace) {
                stream.WriteBool(true);
                stream.WriteBitVec3Coord(cmd.CrosshairTrace);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.CursorUnitEntIndex != last.CursorUnitEntIndex) {
                stream.WriteBool(true);
                stream.WriteBits(cmd.CursorUnitEntIndex, 11);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.QueryEntIndex != last.QueryEntIndex) {
                stream.WriteBool(true);
                stream.WriteBits(cmd.QueryEntIndex, 11);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.CursorUnitEntIndex != last.CursorUnitEntIndex) {
                stream.WriteBool(true);
                stream.WriteBits(cmd.CursorUnitEntIndex, 11);

                if (cmd.QueryEntIndex != last.QueryEntIndex) {
                    stream.WriteBool(true);
                    stream.WriteBits(cmd.QueryEntIndex, 11);
                } else {
                    stream.WriteBool(false);
                }
            } else {
                stream.WriteBool(false);
            }

            if (cmd.MouseDx != last.MouseDx) {
                stream.WriteBool(true);
                stream.WriteUInt16(cmd.MouseDx);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.MouseDy != last.MouseDy) {
                stream.WriteBool(true);
                stream.WriteUInt16(cmd.MouseDy);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.Offset3c != last.Offset3c) {
                stream.WriteBool(true);
                stream.WriteUInt16(cmd.Offset3c);
            } else {
                stream.WriteBool(false);
            }

            // 0x44 to 0x74 inclusive
            if (cmd.OrderId != last.OrderId) {
                stream.WriteBool(true);
                stream.WriteInt16(cmd.OrderId);
                stream.WriteInt16((short) cmd.Order.Value.SelectedUnits.Count);

                foreach (var entity in cmd.Order.Value.SelectedUnits) {
                    stream.WriteBits(entity, 11);
                }

                stream.WriteInt16(cmd.Order.Value.OrderType);
                stream.WriteInt16(cmd.Order.Value.EntityIndex1);
                stream.WriteBitVec3Coord(cmd.Order.Value.PreparedOrderPoint);
                stream.WriteBits(cmd.Order.Value.BaseNpcHandle, 11);
                stream.WriteBool(cmd.Order.Value.QueueOrder);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.CameraX != last.CameraX || cmd.CameraY != last.CameraY) {
                stream.WriteBool(true);
                stream.WriteUInt16(cmd.CameraX);
                stream.WriteUInt16(cmd.CameraY);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.ClickBehavior != last.ClickBehavior) {
                stream.WriteBool(true);
                stream.WriteByte(cmd.ClickBehavior);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.SpectatorStatsSomething != last.SpectatorStatsSomething) {
                stream.WriteBool(true);
                stream.WriteByte(cmd.SpectatorStatsSomething);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.ShopModeSomething != last.ShopModeSomething) {
                stream.WriteBool(true);
                stream.WriteByte(cmd.ShopModeSomething);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.HudStatsDropdownCategoryIndex != last.HudStatsDropdownCategoryIndex) {
                stream.WriteBool(true);
                stream.WriteByte(cmd.HudStatsDropdownCategoryIndex);
            } else {
                stream.WriteBool(false);
            }

            if (cmd.HudStatsDropdownSortMethod != last.HudStatsDropdownSortMethod) {
                stream.WriteBool(true);
                stream.WriteByte(cmd.HudStatsDropdownSortMethod);
            } else {
                stream.WriteBool(false);
            }
        }
    }
}
