using System;
using Microsoft.WindowsAzure.Storage.Table;

public class PostQueueEntity : TableEntity
{
    public PostQueueEntity(string title, string url)
    {
        this.PartitionKey = "PostQueue";
        this.RowKey = Guid.NewGuid().ToString();
    }

    public string Title { get; set; }

    public string Url { get; set; }
    
    public bool Tweeted { get; set; } = false;
}