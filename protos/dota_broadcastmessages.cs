//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#pragma warning disable 1591

// Generated from: dota_broadcastmessages.proto
// Note: requires additional types generated from: google/protobuf/descriptor.proto
// Note: requires additional types generated from: networkbasetypes.proto
namespace nora.protos
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"CDOTABroadcastMsg")]
  public partial class CDOTABroadcastMsg : global::ProtoBuf.IExtensible
  {
    public CDOTABroadcastMsg() {}
    
    private EDotaBroadcastMessages _type;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"type", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public EDotaBroadcastMessages type
    {
      get { return _type; }
      set { _type = value; }
    }

    private byte[] _msg = null;
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"msg", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public byte[] msg
    {
      get { return _msg; }
      set { _msg = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"CDOTABroadcastMsg_LANLobbyRequest")]
  public partial class CDOTABroadcastMsg_LANLobbyRequest : global::ProtoBuf.IExtensible
  {
    public CDOTABroadcastMsg_LANLobbyRequest() {}
    
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"CDOTABroadcastMsg_LANLobbyReply")]
  public partial class CDOTABroadcastMsg_LANLobbyReply : global::ProtoBuf.IExtensible
  {
    public CDOTABroadcastMsg_LANLobbyReply() {}
    

    private ulong _id = default(ulong);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"id", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(ulong))]
    public ulong id
    {
      get { return _id; }
      set { _id = value; }
    }

    private uint _tournament_id = default(uint);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"tournament_id", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(uint))]
    public uint tournament_id
    {
      get { return _tournament_id; }
      set { _tournament_id = value; }
    }

    private uint _tournament_game_id = default(uint);
    [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"tournament_game_id", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(uint))]
    public uint tournament_game_id
    {
      get { return _tournament_game_id; }
      set { _tournament_game_id = value; }
    }
    private readonly global::System.Collections.Generic.List<CDOTABroadcastMsg_LANLobbyReply.CLobbyMember> _members = new global::System.Collections.Generic.List<CDOTABroadcastMsg_LANLobbyReply.CLobbyMember>();
    [global::ProtoBuf.ProtoMember(4, Name=@"members", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public global::System.Collections.Generic.List<CDOTABroadcastMsg_LANLobbyReply.CLobbyMember> members
    {
      get { return _members; }
    }
  

    private bool _requires_pass_key = default(bool);
    [global::ProtoBuf.ProtoMember(5, IsRequired = false, Name=@"requires_pass_key", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(default(bool))]
    public bool requires_pass_key
    {
      get { return _requires_pass_key; }
      set { _requires_pass_key = value; }
    }

    private uint _leader_account_id = default(uint);
    [global::ProtoBuf.ProtoMember(6, IsRequired = false, Name=@"leader_account_id", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(uint))]
    public uint leader_account_id
    {
      get { return _leader_account_id; }
      set { _leader_account_id = value; }
    }
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"CLobbyMember")]
  public partial class CLobbyMember : global::ProtoBuf.IExtensible
  {
    public CLobbyMember() {}
    

    private uint _account_id = default(uint);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"account_id", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(uint))]
    public uint account_id
    {
      get { return _account_id; }
      set { _account_id = value; }
    }

    private string _player_name = "";
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"player_name", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue("")]
    public string player_name
    {
      get { return _player_name; }
      set { _player_name = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
    [global::ProtoBuf.ProtoContract(Name=@"EDotaBroadcastMessages", EnumPassthru=true)]
    public enum EDotaBroadcastMessages
    {
            
      [global::ProtoBuf.ProtoEnum(Name=@"DOTA_BM_LANLobbyRequest", Value=1)]
      DOTA_BM_LANLobbyRequest = 1,
            
      [global::ProtoBuf.ProtoEnum(Name=@"DOTA_BM_LANLobbyReply", Value=2)]
      DOTA_BM_LANLobbyReply = 2
    }
  
}
#pragma warning restore 1591