using System;

public static class GreekCampaign
{
    [Serializable]
    public struct Level
    {
        public int number;
        public string title;
        public string region;
        public string boss;
        public Level(int n, string t, string r, string b = null) { number = n; title = t; region = r; boss = b; }
    }

    public static readonly Level[] Levels =
    {
        new Level(1,  "Il sentiero spezzato",       "Rovine dell'Attica"),
        new Level(2,  "Predoni del tempio",         "Rovine dell'Attica"),
        new Level(3,  "La statua che cammina",      "Rovine dell'Attica"),
        new Level(4,  "Porta dell'oracolo",         "Rovine dell'Attica"),
        new Level(5,  "L'occhio nella rovina",      "Rovine dell'Attica", "Ciclope Guardiano"),

        new Level(6,  "Bosco delle frecce",         "Boschi Sacri di Artemide"),
        new Level(7,  "Caccia al chiaro di luna",   "Boschi Sacri di Artemide"),
        new Level(8,  "Le ninfe corrotte",          "Boschi Sacri di Artemide"),
        new Level(9,  "La radura insanguinata",     "Boschi Sacri di Artemide"),
        new Level(10, "La grande caccia",           "Boschi Sacri di Artemide", "Cinghiale di Calidone"),

        new Level(11, "Serpenti di pietra",         "Tempio delle Gorgoni"),
        new Level(12, "Occhi nella tenebra",        "Tempio delle Gorgoni"),
        new Level(13, "Il giardino delle statue",   "Tempio delle Gorgoni"),
        new Level(14, "Sala dello sguardo proibito", "Tempio delle Gorgoni"),
        new Level(15, "Regina delle Gorgoni",       "Tempio delle Gorgoni", "Medusa"),

        new Level(16, "Ingresso al Labirinto",      "Labirinto Perduto"),
        new Level(17, "Muri che respirano",         "Labirinto Perduto"),
        new Level(18, "Il corridoio senza fine",    "Labirinto Perduto"),
        new Level(19, "Guardiani di Cnosso",        "Labirinto Perduto"),
        new Level(20, "Il custode delle porte",     "Labirinto Perduto", "Asterion il Guardiano"),

        new Level(21, "Il cuore si avvicina",       "Cuore del Labirinto"),
        new Level(22, "Arena degli eroi caduti",    "Cuore del Labirinto"),
        new Level(23, "L'ira di Poseidone",         "Cuore del Labirinto"),
        new Level(24, "L'ultima porta",             "Cuore del Labirinto"),
        new Level(25, "Il Re del Labirinto",        "Cuore del Labirinto", "MINOTAURO")
    };
}
