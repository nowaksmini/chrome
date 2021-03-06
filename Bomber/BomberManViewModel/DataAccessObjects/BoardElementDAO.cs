﻿using System;
using BomberManModel;

namespace BomberManViewModel.DataAccessObjects
{
    /// <summary>
    /// Klasa reprezentująca przekazywane elementy nie poruszające się planszy z widoku do bazy danych i odwrotnie
    /// </summary>
    public class BoardElementDao
    {
        public int Id { get; set; }
        public String Description { get; set; }
        public BoardElementType ElementType { get; set; }
    }
}
