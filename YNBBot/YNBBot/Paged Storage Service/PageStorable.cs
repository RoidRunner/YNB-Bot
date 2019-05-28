using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot.PagedStorageService
{
    internal abstract class PageStorable
    {
        internal int Id;

        internal bool RetrieveId(JSONObject json)
        {
            return json.GetField(ref Id, "Id");
        }
        protected JSONObject IdJSON
        {
            get
            {
                JSONObject json = new JSONObject();
                json.AddField("Id", Id);
                return json;
            }
        }
        internal abstract JSONObject ToJSON();
        internal abstract bool FromJSON(JSONObject json);
    }
}
