using JSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot.PagedStorageService
{
    internal abstract class PageStorable
    {
        internal int Id;

        internal bool RetrieveId(JSONContainer json)
        {
            return json.TryGetField("Id", out Id);
        }
        protected JSONContainer IdJSON
        {
            get
            {
                JSONContainer json = JSONContainer.NewObject();
                json.TryAddField("Id", Id);
                return json;
            }
        }
        internal abstract JSONContainer ToJSON();
        internal abstract bool FromJSON(JSONContainer json);
    }
}
