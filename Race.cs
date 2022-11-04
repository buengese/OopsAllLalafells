using System.Collections.Generic;

namespace OopsAllLalafells {
	public enum Race : byte {
        [Display("Hyur")]
		Hyur = 1,
        [Display("Elezen")]
        Elezen = 2,
        [Display("Lalafell")]
        Lalafell = 3,
        [Display("Miqo'te")]
        Miqote = 4,
        [Display("Roegadyn")]
        Roegadyn = 5,
        [Display("Au Ra")]
        AuRa = 6,
        [Display("Hrothgar")]
        Hrothgar = 7,
        [Display("Viera")]
        Viera = 8
    }

	public class Display : System.Attribute {
		private readonly string _value;

		public Display(string value) {
			_value = value;
		}

		public string Value => _value;
	}

    public class RaceMappings {
	    public static readonly Dictionary<Race, int> RaceHairs = new Dictionary<Race, int> {
		    { Race.Hyur, 13 },
		    { Race.Elezen, 12 },
		    { Race.Lalafell, 13 },
		    { Race.Miqote, 12 },
		    { Race.Roegadyn, 13 },
		    { Race.AuRa, 12 },
		    { Race.Hrothgar, 8 },
		    { Race.Viera, 17 },
	    };
    }
}