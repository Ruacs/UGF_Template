using GameFramework;
using GameFramework.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Lokas
{
    public sealed class CustomEvent : GameEventArgs
    {
        public static readonly int EventId = typeof(CustomEvent).GetHashCode();
        public override int Id
        {
            get
            {
                return EventId;
            }
        }
        public string string_
        {
            get;
            private set;
        }
        public static CustomEvent Create(string string_Name) 
        {
            CustomEvent customEvent = ReferencePool.Acquire<CustomEvent>();
            customEvent.string_ = string_Name;
            return customEvent;
        }
        /// <summary>
        /// 清理
        /// </summary>
        public override void Clear()
        {
            string_ = null;
        }
    }
}
