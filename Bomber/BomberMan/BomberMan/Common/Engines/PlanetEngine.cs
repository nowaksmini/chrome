﻿using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BomberMan.Common.Engines
{
    class PlanetEngine: Engine
    {
        private const int SHIFT = 50;
        public float MaxHeight { get; set; }
        public float MaxWidth { get; set; }

        public PlanetEngine(List<Texture2D> textures, int objectsCount)
            : base(textures, objectsCount)
        {
            for (int i = 0; i < objectsAmount; i++)
            {
                components.Add(GenerateNewPlanet());
            }
        }

        private Component GenerateNewPlanet()
        {
            Texture2D texture = textures[random.Next(textures.Count)];
            float positionY, positionX;
            bool vertical = random.Next(1) > 0;
            if(vertical)
            {
                positionY = random.Next((int)MaxHeight);
                bool left = random.Next(1) > 0;
                if (left)
                    positionX = (float)0;
                else
                    positionX = MaxWidth;
            }
            else
            {
                positionX = random.Next((int)MaxWidth);
                bool up = random.Next(1) > 0;
                if (up)
                    positionY = (float)0;
                else
                    positionY = MaxHeight;
            }
            Vector2 position = new Vector2(positionX, positionY);
            float a = 1f * (float)(random.NextDouble() * 2 - 1);
            float b = 1f * (float)(random.NextDouble() * 2 - 1);
            Vector2 velocity = new Vector2(a,b);
            float angle = MathHelper.Pi - (float)Math.Atan2(a, b);  
            Color color = new Color(
                        (float)random.NextDouble(),
                        (float)random.NextDouble(),
                        (float)random.NextDouble());
            float scale = (float)random.NextDouble() + (float)0.05;
            return new Rocket(texture, position, velocity, angle, color, scale);
        }
    }
}