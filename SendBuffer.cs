﻿// Copyright 2017 Tim Oster. All rights reserved.
// Use of this source code is governed by the MIT license.
// More information can be found in the LICENSE file.

using System;

namespace rmnp
{
	class SendBuffer
	{
		internal enum Operation
		{
			DELETE,
			CANCEL,
			CONTINUE
		}

		internal class SendPacket
		{
			public Packet packet;
			public long sendTime;
			public bool noRTT;
		}

		internal class SendBufferElement
		{
			public SendBufferElement next;
			public SendBufferElement prev;
			public SendPacket data;
		}

		private SendBufferElement head;
		private SendBufferElement tail;
		private readonly object mutex = new object();

		internal void Reset()
		{
			lock (this.mutex)
			{
				this.head = null;
				this.tail = null;
			}
		}

		internal void Add(Packet packet, bool noRTT)
		{
			lock (this.mutex)
			{
				SendBufferElement e = new SendBufferElement();
				e.data = new SendPacket();
				e.data.packet = packet;
				e.data.sendTime = Util.CurrentTime();
				e.data.noRTT = noRTT;

				if (this.head == null)
				{
					this.head = this.tail = e;
				}
				else
				{
					e.prev = this.tail;
					this.tail.next = e;
					this.tail = e;
				}
			}
		}

		private void Remove(SendBufferElement e)
		{
			if (e.prev == null) this.head = e.next;
			else e.prev.next = e.next;

			if (e.next == null) this.tail = e.prev;
			else e.next.prev = e.prev;
		}

		internal SendPacket Retrieve(ushort sequence)
		{
			lock (this.mutex)
			{
				for (SendBufferElement e = this.head; e != null; e = e.next)
				{
					if (e.data.packet.Sequence == sequence)
					{
						this.Remove(e);
						return e.data;
					}
				}

				return null;
			}
		}

		internal void Iterate(Func<int, SendPacket, Operation> iterator)
		{
			lock (this.mutex)
			{
				int i = 0;

				for (SendBufferElement e = this.head; e != null; e = e.next)
				{
					switch (iterator(i++, e.data))
					{
						case Operation.DELETE:
							this.Remove(e);
							break;
						case Operation.CANCEL:
							return;
						case Operation.CONTINUE:
							break;
					}
				}
			}
		}
	}
}
