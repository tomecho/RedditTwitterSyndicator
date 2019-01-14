using System;
using Microsoft.WindowsAzure.Storage.Table;

public class PostQueueEntity : TableEntity
{
    public PostQueueEntity() 
    { 
    }

    public PostQueueEntity(string title, string url)
    {
        this.PartitionKey = "PostQueue";
        this.RowKey = Guid.NewGuid().ToString();
        Title = title;
        Url = url;
    }

    public string Title { get; set; }

    public string Url { get; set; }
    
    public bool Tweeted { get; set; } = false;
}