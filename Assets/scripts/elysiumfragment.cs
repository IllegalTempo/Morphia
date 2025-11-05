using UnityEngine;

public class elsiumfragment : Selectable
{
    public string id;
    [TextArea(3, 10)] // Min 3 lines, max 10 lines
    public string Content;
    
    public override void OnClicked()
    {
        base.OnClicked();
        if(NetworkSystem.instance.IsServer)
        {
            PacketSend.Server_Send_readFragment(id, false);

        } else
        {
            PacketSend.Client_Send_sendReadFragment(id,false);

        }
            gamecore.instance.OnPickEF(id);
    }
    private void Start()
    {
        ItemNameTag tag = Instantiate(gamecore.instance.ItemNameTagPrefab, transform).GetComponent<ItemNameTag>();
        tag.InitializeItemTag("Click ME!");
    }
}
