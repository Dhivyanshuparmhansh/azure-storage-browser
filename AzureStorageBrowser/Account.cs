﻿namespace AzureStorageBrowser
{
    public class Account
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
    }

    public class Blob
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
