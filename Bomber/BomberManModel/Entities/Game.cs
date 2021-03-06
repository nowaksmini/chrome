﻿using System;
using System.Collections.Generic;

namespace BomberManModel.Entities
{
    /// <summary>
    /// Encja reprezentująca całą grę.
    /// </summary>
    public class Game
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int Points { get; set; }
        /// <summary>
        /// Jeżeli gra została zakończona nie pozwala ładować gry użytkownikowi, bierze tylko pod uwagę do najlepszych wyników.
        /// </summary>
        public bool Finished { get; set; }
        /// <summary>
        /// Ostatni czas zapisu gry.
        /// </summary>
        public DateTime SaveTime { get;set; }
        public virtual User User { get; set; }
        public int PlayerXLocation { get; set; }
        public int PlayerYLocation { get; set; }
        public int BombsAmount { get; set; }

        public virtual ICollection<OpponentLocation> OponentLocation { get; set; }
        public virtual ICollection<BoardElementLocation> BoardElementLocation { get; set; }
    }
}
