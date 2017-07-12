using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackofficeSharedData.poco.updates
{
    public class CoverAccount
    {
        private int id;
        private String title;
        private bool active;
        private DateTime created;
        private int office_id;

        public int Id { get => id; set => id = value; }
        public string Title { get => title; set => title = value; }
        public bool Active { get => active; set => active = value; }
        public DateTime Created { get => created; set => created = value; }
        public int Office_id { get => office_id; set => office_id = value; }
    }
}
