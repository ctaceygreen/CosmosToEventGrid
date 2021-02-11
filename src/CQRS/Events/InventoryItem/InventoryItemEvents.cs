﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQRS.Events.InventoryItem
{
    public class InventoryItemDeactivated : Event
    {
        public Guid InventoryId { get; set; }

        public InventoryItemDeactivated(Guid inventoryId) : base(inventoryId)
        {
            InventoryId = inventoryId;
        }
    }

    public class InventoryItemCreated : Event
    {
        public Guid InventoryId { get; set; }
        public string Name { get; set; }
        public InventoryItemCreated(Guid inventoryId, string name):base(inventoryId)
        {
            InventoryId = inventoryId;
            Name = name;
        }
    }

    public class InventoryItemRenamed : Event
    {
        public Guid InventoryId { get; set; }
        public string NewName { get; set; }

        public InventoryItemRenamed(Guid inventoryId, string newName) : base(inventoryId)
        {
            InventoryId = inventoryId;
            NewName = newName;
        }
    }

    public class ItemsCheckedInToInventory : Event
    {
        public Guid InventoryId { get; set; }
        public int Count { get; set; }

        public ItemsCheckedInToInventory(Guid inventoryId, int count) : base(inventoryId)
        {
            InventoryId = inventoryId;
            Count = count;
        }
    }

    public class ItemsRemovedFromInventory : Event
    {
        public Guid InventoryId { get; set; }
        public int Count { get; set; }

        public ItemsRemovedFromInventory(Guid inventoryId, int count) : base(inventoryId)
        {
            InventoryId = inventoryId;
            Count = count;
        }
    }
}
