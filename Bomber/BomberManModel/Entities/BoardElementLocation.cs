﻿using System;

namespace BomberManModel.Entities
{
    /// <summary>
    /// Encja reprezentująca położenie elementów nie poruszających się na planszy.
    /// </summary>
    public class BoardElementLocation
    {
        public int Id { get; set; }
        /// <summary>
        /// Lokalizacja zgodna z oznaczeniami na mapie np punkt 0,0 to pole w lewym górnym rogu planszy
        /// </summary>
        public int XLocation { get; set; }
        public int YLocation { get; set; }
        /// <summary>
        /// Czas w milisekundach ile pozostało życia np bomnie, jeżeli mamy null znaczy, że nie dotyczy np pole planszy Black
        /// </summary>
        public Int64 Timeout { get; set; } 
        public virtual BoardElement BoardElement{ get; set; }
        public virtual Game Game { get; set; }
    }
}
