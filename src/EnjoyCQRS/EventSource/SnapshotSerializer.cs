﻿// The MIT License (MIT)
// 
// Copyright (c) 2016 Nelson Corrêa V. Júnior
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
using System.Collections.Generic;
using EnjoyCQRS.Core;
using EnjoyCQRS.EventSource.Snapshots;

namespace EnjoyCQRS.EventSource
{
    public class SnapshotSerializer : ISnapshotSerializer
    {
        private readonly ITextSerializer _textSerializer;

        public SnapshotSerializer(ITextSerializer textSerializer)
        {
            _textSerializer = textSerializer;
        }

        public ISerializedSnapshot Serialize(IAggregate aggregate, ISnapshot snapshot, IEnumerable<KeyValuePair<string, object>> metadatas)
        {
            var metadata = new Metadata(metadatas);

            var aggregateId = metadata.GetValue(MetadataKeys.AggregateId, (value) => Guid.Parse(value.ToString()));
            var aggregateVersion = metadata.GetValue(MetadataKeys.AggregateSequenceNumber, (value) => int.Parse(value.ToString()));

            var serializedData = _textSerializer.Serialize(snapshot);
            var serializedMetadata = _textSerializer.Serialize(metadata);

            return new SerializedSnapshot(aggregateId, aggregateVersion, serializedData, serializedMetadata, metadata);
        }

        public ISnapshotRestore Deserialize(ICommitedSnapshot commitedSnapshot)
        {
            var metadata = _textSerializer.Deserialize<Metadata>(commitedSnapshot.SerializedMetadata);

            var snapshotClrType = metadata.GetValue(MetadataKeys.SnapshotClrType, (value) => value.ToString());

            var snapshot = (ISnapshot) _textSerializer.Deserialize(commitedSnapshot.SerializedData, snapshotClrType);

            return new SnapshotRestore(commitedSnapshot.AggregateId, commitedSnapshot.AggregateVersion, snapshot, metadata);
        }
    }
}