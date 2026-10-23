namespace Red_Panda_Bazaar_Code.Constant;

public static class MPMessageType {
    // PlayerStall
    public const string PlayerStall_Collect = "PlayerStallCollect";
    public const string PlayerStall_CollectRequest = "PlayerStallCollectRequest";
    public const string PlayerStall_CollectResult = "PlayerStallCollectResult";
    public const string PlayerStall_SyncRequest = "PlayerStallSyncRequest";
    public const string PlayerStall_SyncData = "PlayerStallSyncData";
    public const string PlayerStall_AddItem = "PlayerStallAddItem";
    public const string PlayerStall_AddItemBroadcast = "PlayerStallAddItemBroadcast";
    public const string PlayerStall_RemoveItem = "PlayerStallRemoveItem";
    public const string PlayerStall_RemoveItemBroadcast = "PlayerStallRemoveItemBroadcast";
    public const string PlayerStall_UpdateItem = "PlayerStallUpdateItem";
    public const string PlayerStall_UpdateItemBroadcast = "PlayerStallUpdateItemBroadcast";

    // Bank
    public const string Bank_SyncData = "BankSyncData";
    public const string Bank_ActionRequest = "BankActionRequest";
    public const string Bank_ActionResult = "BankActionResult";

    // SharedBikes
    public const string SharedBikes_SyncData = "SharedBikesSyncData";
    public const string SharedBikes_ActionRequest = "SharedBikesActionRequest";
    public const string SharedBikes_ActionResult = "SharedBikesActionResult";
}
