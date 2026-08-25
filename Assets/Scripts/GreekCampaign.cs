using System;

public static class GreekCampaign
{
    public enum Theme
    {
        AtticaRuins,
        ArtemisWoods,
        GorgonTemple,
        LostLabyrinth,
        LabyrinthHeart
    }

    [Serializable]
    public struct Level
    {
        public int number;
        public string title;
        public string region;
        public Theme theme;
        public int rooms;
        public float difficulty;
        public string featuredEnemy;
        public string boss;

        public Level(int number, string title, string region, Theme theme, int rooms,
            float difficulty, string featuredEnemy, string boss = null)
        {
            this.number = number;
            this.title = title;
            this.region = region;
            this.theme = theme;
            this.rooms = rooms;
            this.difficulty = difficulty;
            this.featuredEnemy = featuredEnemy;
            this.boss = boss;
        }

        public bool IsBossLevel => !string.IsNullOrEmpty(boss);
    }

    public static readonly Level[] Levels =
    {
        new Level(1,  "Il sentiero spezzato",        "Rovine dell'Attica", Theme.AtticaRuins, 5, 1.00f, "Satiro"),
        new Level(2,  "Predoni del tempio",          "Rovine dell'Attica", Theme.AtticaRuins, 5, 1.08f, "Oplita corrotto"),
        new Level(3,  "La statua che cammina",       "Rovine dell'Attica", Theme.AtticaRuins, 6, 1.16f, "Statua vivente"),
        new Level(4,  "Porta dell'oracolo",          "Rovine dell'Attica", Theme.AtticaRuins, 6, 1.25f, "Arpia"),
        new Level(5,  "L'occhio nella rovina",       "Rovine dell'Attica", Theme.AtticaRuins, 7, 1.35f, "Ciclope minore", "Ciclope Guardiano"),

        new Level(6,  "Bosco delle frecce",          "Boschi Sacri di Artemide", Theme.ArtemisWoods, 6, 1.45f, "Cacciatore corrotto"),
        new Level(7,  "Caccia al chiaro di luna",    "Boschi Sacri di Artemide", Theme.ArtemisWoods, 6, 1.55f, "Lupo sacro"),
        new Level(8,  "Le ninfe corrotte",           "Boschi Sacri di Artemide", Theme.ArtemisWoods, 7, 1.66f, "Ninfa corrotta"),
        new Level(9,  "La radura insanguinata",      "Boschi Sacri di Artemide", Theme.ArtemisWoods, 7, 1.78f, "Cervo cornuto"),
        new Level(10, "La grande caccia",            "Boschi Sacri di Artemide", Theme.ArtemisWoods, 8, 1.90f, "Cinghiale sacro", "Cinghiale di Calidone"),

        new Level(11, "Serpenti di pietra",          "Tempio delle Gorgoni", Theme.GorgonTemple, 7, 2.02f, "Serpente del tempio"),
        new Level(12, "Occhi nella tenebra",         "Tempio delle Gorgoni", Theme.GorgonTemple, 7, 2.14f, "Gorgone minore"),
        new Level(13, "Il giardino delle statue",    "Tempio delle Gorgoni", Theme.GorgonTemple, 8, 2.27f, "Statua pietrificata"),
        new Level(14, "Sala dello sguardo proibito", "Tempio delle Gorgoni", Theme.GorgonTemple, 8, 2.40f, "Sacerdotessa serpente"),
        new Level(15, "Regina delle Gorgoni",        "Tempio delle Gorgoni", Theme.GorgonTemple, 9, 2.55f, "Gorgone elite", "Medusa"),

        new Level(16, "Ingresso al Labirinto",       "Labirinto Perduto", Theme.LostLabyrinth, 8, 2.70f, "Guardia di Cnosso"),
        new Level(17, "Muri che respirano",          "Labirinto Perduto", Theme.LostLabyrinth, 8, 2.86f, "Spirito del labirinto"),
        new Level(18, "Il corridoio senza fine",     "Labirinto Perduto", Theme.LostLabyrinth, 9, 3.02f, "Lama vivente"),
        new Level(19, "Guardiani di Cnosso",         "Labirinto Perduto", Theme.LostLabyrinth, 9, 3.20f, "Bruto taurino"),
        new Level(20, "Il custode delle porte",      "Labirinto Perduto", Theme.LostLabyrinth, 10, 3.40f, "Minotauro minore", "Asterion il Guardiano"),

        new Level(21, "Il cuore si avvicina",        "Cuore del Labirinto", Theme.LabyrinthHeart, 9, 3.60f, "Guardiano taurino"),
        new Level(22, "Arena degli eroi caduti",     "Cuore del Labirinto", Theme.LabyrinthHeart, 10, 3.82f, "Eroe caduto"),
        new Level(23, "L'ira di Poseidone",          "Cuore del Labirinto", Theme.LabyrinthHeart, 10, 4.05f, "Spirito marino"),
        new Level(24, "L'ultima porta",              "Cuore del Labirinto", Theme.LabyrinthHeart, 11, 4.30f, "Campione di Cnosso"),
        new Level(25, "Il Re del Labirinto",         "Cuore del Labirinto", Theme.LabyrinthHeart, 12, 4.60f, "Guardia reale", "Minotauro")
    };

    public static Level Get(int number)
    {
        int index = Math.Max(0, Math.Min(Levels.Length - 1, number - 1));
        return Levels[index];
    }
}
