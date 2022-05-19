using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace toc_arena.Models
{
    public class CloudEvent<T>
    {
        public string Type { get; set; }
        public string Topic { get; set; }
        public string TraceId { get; set; }
        public string SpecVersion { get; set; }
        public string DataContentType { get; set; }
        public string Source { get; set; }
        public string PubSubName { get; set; }
        public string TraceState { get; set; }
        public string Id { get; set; }
        public T Data { get; set; }

    }
}