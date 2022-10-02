using System;
using System.Runtime.Serialization;

namespace AsapWiki.Shared.Models
{
	public partial class ProcessingInstruction
	{
		#region Properties

		private int _pageId;
		public int PageId
		{
			get
			{
				return this._pageId;
			}
			set
			{
				if (this._pageId != value)
				{
					this._pageId = value;
				}
			}
		}

		private string _instruction;
		public string Instruction
		{
			get
			{
				return this._instruction;
			}
			set
			{
				if (this._instruction != value)
				{
					this._instruction = value;
				}
			}
		}


		#endregion
	}
}
