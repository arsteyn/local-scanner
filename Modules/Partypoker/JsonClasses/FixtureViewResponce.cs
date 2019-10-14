using System;
using System.Collections.Generic;
using System.Security.AccessControl;

namespace Partypoker.JsonClasses
{
    public class FixtureViewResponce
    {
        public Fixture fixture { get; set; }
    }

    public class Fixture
    {
        public List<Participant> participants { get; set; }

        public long id { get; set; }

        public string stage { get; set; }

        public Scoreboard scoreboard { get; set; }

        public List<Game> games { get; set; }

        public Sport sport { get; set; }

        public Region region { get; set; }

        public DateTime startDate { get; set; }

        //public string cutOffDate { get; set; }

        public Competition competition { get; set; }

        public Name name { get; set; }

    }

    public class Competition : Region
    {
        public long parentLeagueId { get; set; }
    }

    public class Region
    {
        public int sportId { get; set; }

        public string type { get; set; }

        public int id { get; set; }

        public int parentId { get; set; }

        public Name name { get; set; }
    }

    public class Sport
    {
        public int id { get; set; }

        public Name name { get; set; }
    }

    public class Game
    {
        public long id { get; set; }

        public Name name { get; set; }

        public List<Result> results { get; set; }

        public string visibility { get; set; }
    }

    public class Result
    {
        public long id { get; set; }
        public decimal odds { get; set; }
        public Name name { get; set; }
        public Name sourceName { get; set; }
        public string visibility { get; set; }
    }

    public class Scoreboard
    {
        //"2:3"
        public string score { get; set; }

        //"2H"
        public string period { get; set; }
    }

    public class Participant
    {
        public Name name { get; set; }
    }

    public class Name
    {
        public string value { get; set; }
        public string sign { get; set; }
    }
}