using System;

public static class HeroCatalog
{
    public enum HeroId
    {
        Perseus,
        Heracles,
        Atalanta,
        Achilles,
        Theseus,
        Odysseus,
        Medea,
        Orpheus
    }

    [Serializable]
    public struct Hero
    {
        public HeroId id;
        public string displayName;
        public string title;
        public string weapon;
        public string passive;
        public string ultimate;
        public int unlockLevel;
        public int unlockCost;
        public float hp;
        public float damage;
        public float moveSpeed;
        public float attackCooldown;

        public Hero(HeroId id, string displayName, string title, string weapon, string passive,
            string ultimate, int unlockLevel, int unlockCost, float hp, float damage,
            float moveSpeed, float attackCooldown)
        {
            this.id = id;
            this.displayName = displayName;
            this.title = title;
            this.weapon = weapon;
            this.passive = passive;
            this.ultimate = ultimate;
            this.unlockLevel = unlockLevel;
            this.unlockCost = unlockCost;
            this.hp = hp;
            this.damage = damage;
            this.moveSpeed = moveSpeed;
            this.attackCooldown = attackCooldown;
        }
    }

    public static readonly Hero[] Heroes =
    {
        new Hero(HeroId.Perseus, "Perseo", "Uccisore di mostri", "Mythbow",
            "Dopo una schivata il prossimo colpo ha probabilità critica aumentata.",
            "Egida di Atena: breve invulnerabilità e raffica divina.",
            1, 0, 120f, 28f, 7.0f, 0.62f),

        new Hero(HeroId.Heracles, "Eracle", "Forza dell'Olimpo", "Clava di Nemea",
            "Più il nemico è vicino, maggiore è il danno inflitto.",
            "Furia dei Dodici Lavori: schianto circolare ad area.",
            3, 800, 165f, 38f, 5.8f, 0.82f),

        new Hero(HeroId.Atalanta, "Atalanta", "Cacciatrice di Artemide", "Arco lunare",
            "La velocità d'attacco aumenta mentre non subisce danni.",
            "Pioggia di Artemide: salva di frecce su tutta l'arena.",
            6, 1300, 105f, 24f, 8.2f, 0.50f),

        new Hero(HeroId.Achilles, "Achille", "Invincibile di Ftia", "Lancia e scudo",
            "Riduce fortemente il primo danno ricevuto in ogni stanza.",
            "Carica di Peleo: assalto in linea che perfora i nemici.",
            9, 1800, 150f, 34f, 6.5f, 0.70f),

        new Hero(HeroId.Theseus, "Teseo", "Signore del Labirinto", "Lama di Atene",
            "Infligge danni extra a bestie, guardiani e creature taurine.",
            "Filo di Arianna: rallenta tutti e marca il bersaglio più pericoloso.",
            12, 2200, 130f, 31f, 7.1f, 0.64f),

        new Hero(HeroId.Odysseus, "Odisseo", "Re dell'astuzia", "Arco di Itaca",
            "Ogni pochi colpi crea una trappola sul terreno.",
            "Nessuno: crea un'esca e diventa non bersagliabile per breve tempo.",
            15, 2600, 115f, 27f, 7.2f, 0.58f),

        new Hero(HeroId.Medea, "Medea", "Strega della Colchide", "Bastone rituale",
            "Gli effetti elementali durano più a lungo e possono diffondersi.",
            "Cerchio di Ecate: grande area di fuoco e veleno.",
            18, 3200, 100f, 30f, 6.7f, 0.66f),

        new Hero(HeroId.Orpheus, "Orfeo", "Voce che piega l'Ade", "Lira sacra",
            "Alcuni nemici colpiti vengono rallentati o confusi.",
            "Canto degli Inferi: onda sonora che respinge e stordisce.",
            21, 3800, 110f, 26f, 6.9f, 0.56f)
    };

    public static Hero Get(HeroId id)
    {
        for (int i = 0; i < Heroes.Length; i++)
            if (Heroes[i].id == id) return Heroes[i];
        return Heroes[0];
    }
}
