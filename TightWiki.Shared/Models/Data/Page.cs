using System;

namespace TightWiki.Shared.Models.Data
{
    public partial class Page
    {
        #region Properties

        private int _id;
        public int Id
        {
            get
            {
                return this._id;
            }
            set
            {
                if (this._id != value)
                {
                    this._id = value;
                }
            }
        }

        private string _name;
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if (this._name != value)
                {
                    this._name = value;
                }
            }
        }

        private string _navigation;
        public string Navigation
        {
            get
            {
                return this._navigation;
            }
            set
            {
                if (this._navigation != value)
                {
                    this._navigation = value;
                }
            }
        }

        private string _description;
        public string Description
        {
            get
            {
                return this._description;
            }
            set
            {
                if (this._description != value)
                {
                    this._description = value;
                }
            }
        }

        private int _revision;
        public int Revision
        {
            get
            {
                return this._revision;
            }
            set
            {
                if (this._revision != value)
                {
                    this._revision = value;
                }
            }
        }

        private int _createdByUserId;
        public int CreatedByUserId
        {
            get
            {
                return this._createdByUserId;
            }
            set
            {
                if (this._createdByUserId != value)
                {
                    this._createdByUserId = value;
                }
            }
        }

        private DateTime _createdDate;
        public DateTime CreatedDate
        {
            get
            {
                return this._createdDate;
            }
            set
            {
                if (this._createdDate != value)
                {
                    this._createdDate = value;
                }
            }
        }

        private int _modifiedByUserId;
        public int ModifiedByUserId
        {
            get
            {
                return this._modifiedByUserId;
            }
            set
            {
                if (this._modifiedByUserId != value)
                {
                    this._modifiedByUserId = value;
                }
            }
        }

        private DateTime _modifiedDate;
        public DateTime ModifiedDate
        {
            get
            {
                return this._modifiedDate;
            }
            set
            {
                if (this._modifiedDate != value)
                {
                    this._modifiedDate = value;
                }
            }
        }

        #endregion
    }
}
