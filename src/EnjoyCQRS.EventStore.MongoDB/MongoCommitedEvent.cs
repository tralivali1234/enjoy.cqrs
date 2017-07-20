// The MIT License (MIT)
// 
// Copyright (c) 2016 Nelson Corr�a V. J�nior
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using EnjoyCQRS.Events;
using EnjoyCQRS.EventSource;

namespace EnjoyCQRS.EventStore.MongoDB
{
    internal class MongoCommitedEvent : ICommitedEvent
    {
        public Guid AggregateId { get; set; }
        public int Version { get; set; }
        public object Data { get; set; }
        public IMetadata Metadata { get; set; }

        public MongoCommitedEvent(Guid aggregateId, int version, object data, IMetadata metadata)
        {
            AggregateId = aggregateId;
            Version = version;
            Data = data;
            Metadata = metadata;
        }
    }
}