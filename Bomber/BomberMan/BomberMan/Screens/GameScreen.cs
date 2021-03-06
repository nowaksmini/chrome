﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BomberMan.Common;
using BomberMan.Common.Components.StateComponents;
using BomberMan.Common.Engines;
using BomberManModel;
using BomberManModel.Entities;
using BomberManViewModel;
using BomberManViewModel.DataAccessObjects;
using BomberManViewModel.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BomberMan.Screens
{
    /// <summary>
    /// Klasa tworząca cały widok gry.
    /// </summary>
    public class GameScreen : Screen
    {
        private const int SimpleLevelRows = 12;
        private const int SimpleLevelColumns = 18;
        private const int MediumLevelRows = 14;
        private const int MediumLevelComulns = 20;
        private const int HighLevelRows = 18;
        private const int HighLevelColumns = 22;
        private const int SuperLevelRows = 22;
        private const int SuperLevelColumns = 32;
        private const int MaxNumberOfLevel = 19;
        private const int StartBombAmount = 4;

        private const int ButtonResetGap = 10;
        private const float ButtonRestarWidth = 200;
        private const float SpectialButtonsHeight = 90;
        private const float BonusSize = 60;

        private const string Level = "Poziom";
        private const string Points = "Punkty";
        private const string TryAgain = "SPRóBUJ JESZCZE RAZ";
        private const string BravoWinning = "BRAVO!!!!";
        private const string BonusLabel = "Bonusy :";

        private const int PercentageOfSolidBlocks = 7;
        private const int PercentageOfGreyBlocks = 30;
        private const int PercentageOfBonuses = 6;
        private const int PercentageOfOpponents = 3;

        //Przyznawane punkty
        private const int DeletingGreyField = 10;
        private const int DeletingOctopus = 150;
        private const int DeletingGhoast = 300;
        private const int PointBonus = 50;

        //Stałe cykle timerów
        private const float FastBonusDuration = 3f; //3s
        private const float SlowBonusDuration = 9f;
        private const float StrengthBonusDuration = 10f;
        private const float InmortalBonusDuration = 10f;
        private const float BombCycleDuration = 5.0f;
        private const float RedBlockCycleDuration = 1.0f;
        private const float PLayerMoveDurationCycle = 0.1f; //every  0.3s.
        private const float OpponentMoveCycyle = 1f;
        private const float StartInmortality = 5f;

        private int _rows;
        private int _columns;
        private readonly Random _random;
        private BoardEngine _boardEngine;
        private readonly Label _informationLabel;
        private readonly SpriteFont _titleSpriteFont;


        /// <summary>
        /// Lista zawierająca wszystkie jednostkowe pola planszy ze wzgędu na typ pola
        /// Przekazywana do BoardEngine w celu narysowania jednostkowych pól
        /// </summary>
        private List<BlockType> _boardBlocksTypes;

        private Dictionary<int, BonusType> _bonusLocations;
        // pozycja pola, lista postaci na danym polu wraz i ostatnim polem gdzie widziano gracza
        private Dictionary<int, List<Tuple<CharacterType, int>>> _characterLocations;
        private List<int> _bombLocations;
        private readonly Label _levelLabel;
        private readonly Label _bonusLabel;
        private int _bombAmount;

        /// <summary>
        /// Przechowywane textury ładowane podczas włączania gry
        /// Jedna textura na jeden obrazek
        /// </summary>
        private readonly List<Texture2D> _blockTextures;

        private readonly Texture2D _bombTexture;
        private readonly List<Texture2D> _characterTextures;
        private readonly List<Texture2D> _bonusesTextures;
        private Button _backButton;
        private Button _helpButton;
        private readonly Button _saveButton;
        private readonly Button _restartGame;

        //pomocnicze timery
        private float _currentPlayerMoveTimeCycle;
        private float _currentPlayerMoveTime;
        private float _currentOpponentMoveTimeCycyle;
        private float _currentOpponentMoveTime;
        private List<float> _currentBombTimes;
        private float _currentRedBlockTime;

        private float _currentFastBonusTime;
        private float _currentFastBonusCycle;
        private float _currentSlowBonusTime;
        private float _currentSlowBonusCycle;
        private float _currentStrengthBonusTime;
        private float _currentStrengthBonusCycle;
        private float _currentInmortalBonusTime;
        private float _currentInmortalBonusCycle;

        private bool _isSuperBomb;
        private bool _isInmortal;
        private bool _shouldUpdate;

        /// <summary>
        /// Utwórz widok gry ze wszystkimi polami jednostkowymi
        /// Jeżeli nie ma utworzonego GameDAO w Utils to wygeneruj nową grę z poziomem 0
        /// W przeciwnym przypadku załąduj grę z Utils i utwórz widok całej planszy
        /// </summary>
        public GameScreen(List<Texture2D> blockTextures, List<Texture2D> bonusesTextures,
            Texture2D bombTexture, List<Texture2D> characterTextures, Texture2D backButtonTexture,
            SpriteFont titleFont, Texture2D saveButtonTexture, Texture2D startAgainTexture, 
            Texture2D helpButton)
        {
            _titleSpriteFont = titleFont;
            _informationLabel = new Label(titleFont, "", Color.BlueViolet);
            _saveButton = new Button(saveButtonTexture, Color.White, null, "", Color.White)
            {
                Click = delegate()
                {
                    SaveGame();
                    return Color.Transparent;
                }
            };
            _restartGame = new Button(startAgainTexture, Color.White, null, "", Color.White)
            {
                Click = delegate()
                {
                    _informationLabel.Text = "";
                    Utils.Game.Id = -1;
                    CreateNewGame();
                    return Color.Transparent;
                }
            };
            _currentBombTimes = new List<float>();
            _bonusesTextures = bonusesTextures;
            _blockTextures = blockTextures;
            _bombTexture = bombTexture;
            _characterTextures = characterTextures;
            _random = new Random();
            _boardBlocksTypes = new List<BlockType>();
            _bonusLocations = new Dictionary<int, BonusType>();
            _bombLocations = new List<int>();
            _characterLocations = new Dictionary<int, List<Tuple<CharacterType, int>>>();
            CreateBackButton(backButtonTexture);
            CreateHelpButton(helpButton);
            if (Utils.Game == null)
            {
                Utils.Game = CreateNewGame();
            }
            _levelLabel = new Label(titleFont, Level + " " + (Utils.Game.Level + 1) + " " + Points + Utils.Game.Points
                , Color.White);
            _bonusLabel = new Label(titleFont, BonusLabel, Color.White);
        }

        #region loadGame

        /// <summary>
        /// Załaduj grę
        /// </summary>
        public void LoadGame()
        {
            _informationLabel.Text = "";
            _shouldUpdate = false;
            String message;
            Utils.Game = GameService.GetGameForUserById(Utils.User, Utils.Game.Id, out message);
            _boardBlocksTypes = new List<BlockType>();
            _bombLocations = new List<int>();
            _currentPlayerMoveTimeCycle = PLayerMoveDurationCycle;
            _currentOpponentMoveTimeCycyle = OpponentMoveCycyle;
            _currentBombTimes = new List<float>();
            _currentFastBonusTime = 0f;
            _currentFastBonusCycle = 0f;
            _currentOpponentMoveTime = 0f;
            _currentPlayerMoveTime = 0f;
            _currentSlowBonusCycle = 0f;
            _currentSlowBonusTime = 0f;
            _currentStrengthBonusCycle = 0f;
            _currentStrengthBonusTime = 0f;
            _currentInmortalBonusCycle = StartInmortality;
            _isInmortal = true;
            _isSuperBomb = false;
            _currentInmortalBonusTime = 0f;
            _currentRedBlockTime = 0f;
            _bombAmount = Utils.Game.BombsAmount;
            int level = Utils.Game.Level;
            int columns, rows;
            if (level < 0 || level > MaxNumberOfLevel)
                throw new NotImplementedException("Level Should be between indexes 0 and " + MaxNumberOfLevel);
            if (level < 5)
            {
                columns = SimpleLevelColumns;
                rows = SimpleLevelRows;
            }
            else if (level < 10)
            {
                columns = MediumLevelComulns;
                rows = MediumLevelRows;
            }
            else if (level < 15)
            {
                columns = HighLevelColumns;
                rows = HighLevelRows;
            }
            else
            {
                columns = SimpleLevelColumns;
                rows = SimpleLevelRows;
            }
            _boardEngine.Rows = rows;
            _boardEngine.Columns = columns;
            _rows = rows;
            _columns = columns;
            LoadBlocks();
            LoadBonuses();
            LoadBombs();
            LoadOpponents();
            int playerPosition = (int) (Utils.Game.PlayerXLocation*_columns + Utils.Game.PlayerYLocation);
            _boardEngine.PlayerLocation = playerPosition;
            if (!_characterLocations.ContainsKey(playerPosition))
            {
                _characterLocations.Add(playerPosition, new List<Tuple<CharacterType, int>>());
            }
            _characterLocations[playerPosition].Add(new Tuple<CharacterType,int>(CharacterType.Player, -1));
            _shouldUpdate = true;
        }

        /// <summary>
        /// Pobierz z bazy informacje na temat kolorów pól planszy.
        /// </summary>
        private void LoadBlocks()
        {
            _boardBlocksTypes.Clear();
            String message;
            for (int i = 0; i < _rows * _columns; i++)
            {
               _boardBlocksTypes.Add(BlockType.White);
            }
            List<BoardElementLocationDao> blocks = BoardService.GetAllBlocksForGame(Utils.Game, out message);
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockType blockKind = BlockType.White;
                switch (blocks[i].BoardElement.ElementType)
                {
                    case BoardElementType.WhiteBlock:
                        blockKind = BlockType.White;
                        break;
                    case BoardElementType.RedBlock:
                        if (blocks[i].Timeout != null)
                        {
                            _currentRedBlockTime = (float) blocks[i].Timeout;
                        }
                        blockKind = BlockType.Red;
                        break;
                    case BoardElementType.GrayBlock:
                        blockKind = BlockType.Grey;
                        break;
                    case BoardElementType.BlackBlock:
                        blockKind = BlockType.Black;
                        break;
                }
                _boardBlocksTypes[blocks[i].XLocation * _columns + blocks[i].YLocation] = blockKind;
            }
        }

        /// <summary>
        /// Pobierz z bazy informacje na temat rozmieszczenia bonusów i aktualnie posiadanych.
        /// </summary>
        private void LoadBonuses()
        {
            _bonusLocations.Clear();
            String message;
            List<BoardElementLocationDao> bonuses = BoardService.GetAllBonusesForGame(Utils.Game, out message);
            for (int i = 0; i < bonuses.Count; i++)
            {
                BonusType bonusType = BonusType.BombAmount;
                switch (bonuses[i].BoardElement.ElementType)
                {
                    case BoardElementType.PointBonus:
                        bonusType = BonusType.Points;
                        break;
                    case BoardElementType.BombAmountBonus:
                        bonusType = BonusType.BombAmount;
                        break;
                    case BoardElementType.InmortalBonus:
                        bonusType = BonusType.Inmortal;
                        break;
                    case BoardElementType.StrenghtBonus:
                        bonusType = BonusType.Strenght;
                        break;
                    case BoardElementType.FastBonus:
                        bonusType = BonusType.Fast;
                        break;
                    case BoardElementType.SlowBonus:
                        bonusType = BonusType.Slow;
                        break;
                }
                if (bonuses[i].XLocation == -1 || bonuses[i].YLocation == -1)
                {
                    if (bonuses[i].Timeout != null)
                    {
                        switch (bonusType)
                        {
                            case BonusType.Fast:
                                _currentFastBonusCycle += (float) bonuses[i].Timeout;
                                break;
                            case BonusType.Slow:
                                _currentSlowBonusCycle += (float)bonuses[i].Timeout;
                                break;
                            case BonusType.Inmortal:
                                _currentInmortalBonusCycle += (float)bonuses[i].Timeout;
                                break;
                            case BonusType.Strenght:
                                _currentStrengthBonusCycle += (float)bonuses[i].Timeout;
                                break;
                        }
                    }
                }
                else
                {
                    int position = bonuses[i].XLocation*_columns + bonuses[i].YLocation;
                    if(!_bonusLocations.ContainsKey(position))
                        _bonusLocations.Add(position, bonusType);
                }
            }
        }

        /// <summary>
        /// Pobierz z bazy informacje o rozmieszczeniu bomb.
        /// </summary>
        private void LoadBombs()
        {
            _bombLocations.Clear();
            String message;
            List<BoardElementLocationDao> bombs = BoardService.GetAllBombsForGame(Utils.Game, out message);
            foreach (var bomb in bombs)
            {
                if (bomb.XLocation != -1 && bomb.YLocation != -1)
                {
                    _bombLocations.Add(bomb.XLocation * _columns + bomb.YLocation);
                    _currentBombTimes.Add(bomb.XLocation * _columns + bomb.YLocation);
                }
            }
        }

        /// <summary>
        /// Pobierz z bazy informacje na temat pozcji przeciwników.
        /// </summary>
        private void LoadOpponents()
        {
            _characterLocations.Clear();
            String message;
            List<OpponentLocationDao> opponentLocationDaos =
                OpponentService.GetAllOponentsWithLocationsByGame(Utils.Game, out message);
            foreach (var opponent in opponentLocationDaos)
            {
                CharacterType characterType = CharacterType.Octopus;
                switch (opponent.Oponent.OpponentType)
                {
                    case OpponentType.Ghost:
                        characterType = CharacterType.Ghost;
                        break;
                    case OpponentType.Octopus:
                        characterType = CharacterType.Octopus;
                        break;
                }
                int position = (int)(opponent.XLocation*_columns + opponent.YLocation);
                if (!_characterLocations.ContainsKey(position))
                {
                    _characterLocations.Add(position, new List<Tuple<CharacterType, int>>());
                }
                _characterLocations[position].Add(new Tuple<CharacterType, int>(characterType, -1));
            }
        }

        #endregion

        /// <summary>
        /// Utwórz przycisk powrotu do głównego menu.
        /// </summary>
        /// <param name="backButtonTexture">tło przycisku powrotu</param>
        private void CreateBackButton(Texture2D backButtonTexture)
        {
            _backButton = new Button(backButtonTexture, Color.White, null, "", Color.White)
            {
                Click = delegate()
                {
                    GameManager.ScreenType = ScreenType.MainMenu;
                    return Color.Transparent;
                }
            };
            _backButton.Scale = new Vector2(GameManager.BackButtonSize/_backButton.Texture.Width,
                GameManager.BackButtonSize/_backButton.Texture.Height);
            _backButton.Position = new Vector2(GameManager.BackButtonSize/2, GameManager.BackButtonSize/2);
        }

        /// <summary>
        /// Utwórz przycisk pomocy
        /// </summary>
        /// <param name="helpButtonTexture">tło przycisku pomocy</param>
        private void CreateHelpButton(Texture2D helpButtonTexture)
        {
            _helpButton = new Button(helpButtonTexture, Color.White, null, "", Color.White)
            {
                Click = delegate()
                {
                    GameManager.ScreenType = ScreenType.Help;
                    return Color.Transparent;
                }
            };
            float width = GameManager.BackButtonSize;
            float height = GameManager.BackButtonSize - 5;
            _helpButton.Scale = new Vector2(width / _backButton.Texture.Width,
                height/ _backButton.Texture.Height);
            _helpButton.Position = new Vector2(GameManager.BackButtonSize * 3 / 2, GameManager.BackButtonSize / 2);
        }

        /// <summary>
        /// Narysuj wsyztskie komponenty w oknie gry.
        /// </summary>
        /// <param name="spriteBatch">Obiekt, w którym rysowane są komponenty.</param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            _boardEngine.Draw(spriteBatch);
            spriteBatch.Begin();
            _backButton.Draw(spriteBatch);
            _helpButton.Draw(spriteBatch);
            _levelLabel.Draw(spriteBatch);
            _restartGame.Draw(spriteBatch);
            _saveButton.Draw(spriteBatch);
            _informationLabel.Draw(spriteBatch);
            _bonusLabel.Draw(spriteBatch);
            // dorysuj na dole wszystkie bonusy czasowe aktualnie posiadane
            DrawBonuses(spriteBatch);
            spriteBatch.End();
        }

        /// <summary>
        /// Narysuj wszytskie bonusy w infomacji.
        /// </summary>
        /// <param name="spriteBatch">Obiekt, na którym rysujemy.</param>
        private void DrawBonuses(SpriteBatch spriteBatch)
        {
            float x = _bonusLabel.Position.X + _titleSpriteFont.MeasureString(_bonusLabel.Text).X;
            float y = _bonusLabel.Position.Y + _titleSpriteFont.MeasureString(_bonusLabel.Text).Y/2;
            if (_currentInmortalBonusCycle > 0f)
            {
                x += BonusSize;
                DrawBonus(spriteBatch, BonusType.Inmortal, x, y);
            }
            if (_currentFastBonusCycle > 0f)
            {
                x += BonusSize;
                DrawBonus(spriteBatch, BonusType.Fast, x, y);
            }
            if (_currentSlowBonusCycle > 0f)
            {
                x += BonusSize;
                DrawBonus(spriteBatch, BonusType.Slow, x, y);
            }
            if (_currentStrengthBonusCycle > 0f)
            {
                x += BonusSize;
                DrawBonus(spriteBatch, BonusType.Strenght, x, y);
            }
        }

        /// <summary>
        /// Narysuj bonus.
        /// </summary>
        /// <param name="spriteBatch">Obiekt, na którym rysujemy</param>
        /// <param name="bonusType">Rodzaj bonusa</param>
        /// <param name="x">Położenie x bonusa.</param>
        /// <param name="y">Położenie y bonusa.</param>
        private void DrawBonus(SpriteBatch spriteBatch, BonusType bonusType, float x, float y)
        {
            // narysuj białe tło
            Vector2 blockScale = new Vector2(BonusSize/_blockTextures[(int) BlockType.White].Width,
                BonusSize/_blockTextures[(int) BlockType.White].Height);
            Rectangle sourceBlockRectangle = new Rectangle
                (0, 0, _blockTextures[(int) BlockType.White].Width,
                    _blockTextures[(int) BlockType.White].Height);
            Vector2 blockOrigin = new Vector2((float) _blockTextures[(int) BlockType.White].Width/2,
                (float) _blockTextures[(int) BlockType.White].Height/2);
            spriteBatch.Draw(_blockTextures[(int) BlockType.White], new Vector2(x, y),
                sourceBlockRectangle, Color.White, 0.0f, blockOrigin, blockScale, SpriteEffects.None, 0f);

            Vector2 scale = new Vector2(BonusSize/_bonusesTextures[(int) bonusType].Width,
                BonusSize/_bonusesTextures[(int) bonusType].Height);
            Rectangle sourceRectangle = new Rectangle
                (0, 0, _bonusesTextures[(int) bonusType].Width, _bonusesTextures[(int) bonusType].Height);
            Vector2 origin = new Vector2((float) _bonusesTextures[(int) bonusType].Width/2,
                (float) _bonusesTextures[(int) bonusType].Height/2);
            spriteBatch.Draw(_bonusesTextures[(int) bonusType], new Vector2(x, y),
                sourceRectangle, Color.White, 0.0f, origin, scale, SpriteEffects.None, 0f);
        }

        #region update

        /// <summary>
        /// Uaktualnij widok planszy w zależności od rozmiaru okna gry
        /// </summary>
        /// <param name="gameTime">Czas gry</param>
        /// <param name="windowWidth">Szerokość okna</param>
        /// <param name="windowHeight">Wysokość okna</param>
        public override void Update(GameTime gameTime, int windowWidth, int windowHeight)
        {
            try
            {
                float gameTimeSeconds = (float) gameTime.ElapsedGameTime.TotalSeconds;
                double frameTime = gameTime.ElapsedGameTime.Milliseconds/1000.0;
                MouseState mouseState = Mouse.GetState();
                PrevMousePressed = MousePressed;
                MousePressed = mouseState.LeftButton == ButtonState.Pressed;
                _backButton.Update(mouseState.X, mouseState.Y, frameTime, MousePressed, PrevMousePressed);
                _helpButton.Update(mouseState.X, mouseState.Y, frameTime, MousePressed, PrevMousePressed);
                if (_shouldUpdate)
                {
                    _currentRedBlockTime += gameTimeSeconds;
                    // sprawdź czy na czerwonych polach są jakieś postacie
                    if (_currentRedBlockTime >= RedBlockCycleDuration)
                    {
                        _currentRedBlockTime -= RedBlockCycleDuration;
                        for (int i = 0; i < _boardBlocksTypes.Count; i++)
                        {
                            if (_boardBlocksTypes[i] == BlockType.Red)
                            {
                                DeleteCharacters(i);
                                _boardBlocksTypes[i] = BlockType.White;
                            }
                        }
                    }
                    // uaktulanij czas wybuchu bomb
                    UpdateBombs(gameTimeSeconds);
                    // akualizacja czasu ruchu przeciwników
                    UpdateCharactersLocations(gameTimeSeconds);
                    CheckIfPlayerIsOnSameFieldAsAnyOpponent();
                    //sprawdź wszystkie bonusy
                    UpdateBonusesDuration(gameTimeSeconds);
                    CheckIfUserShouldGetAnyBonus();
                    CheckIfUserWonLevel();
                }
                _boardEngine.Update(_boardBlocksTypes, _bonusLocations, _characterLocations, _bombLocations, windowWidth,
                    windowHeight);
                //uaktualnij rozmairy i pozycje labelek i przycisków
                _restartGame.Scale = new Vector2(ButtonRestarWidth/_restartGame.Texture.Width,
                    SpectialButtonsHeight/_restartGame.Texture.Height);
                _restartGame.Position = new Vector2(windowWidth - ButtonRestarWidth/2,
                    SpectialButtonsHeight/2 - ButtonResetGap);
                _saveButton.Scale = new Vector2(SpectialButtonsHeight/_saveButton.Texture.Width,
                    SpectialButtonsHeight/_saveButton.Texture.Height);
                _saveButton.Position = new Vector2(_restartGame.Position.X - SpectialButtonsHeight,
                    SpectialButtonsHeight/2);
                _levelLabel.Position = new Vector2(GameManager.BackButtonSize*2, 0);
                _restartGame.Update(mouseState.X, mouseState.Y, frameTime, MousePressed, PrevMousePressed);
                _saveButton.Update(mouseState.X, mouseState.Y, frameTime, MousePressed, PrevMousePressed);
                _informationLabel.Position =
                    new Vector2((float) windowWidth/2 - _titleSpriteFont.MeasureString(_informationLabel.Text).X/2,
                        (float) windowHeight/2 - _titleSpriteFont.MeasureString(_informationLabel.Text).Y/2);
                _levelLabel.Text = Level + " " + (Utils.Game.Level + 1) + " " + Points + " " + Utils.Game.Points;
                _bonusLabel.Position = new Vector2(_titleSpriteFont.MeasureString(_bonusLabel.Text).X,
                    windowHeight - _titleSpriteFont.MeasureString(_bonusLabel.Text).Y);
            }
            catch (Exception e)
            {
                var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
                if (declaringType != null)
                    Logger.LogMessage(declaringType.Name, MethodBase.GetCurrentMethod().Name,
                        e.StackTrace);
            }
        }

        /// <summary>
        /// Sprawdź czy gracz wygrał poziom i należy załadować kolejny.
        /// </summary>
        private void CheckIfUserWonLevel()
        {
            if (_characterLocations.Keys.Count == 1)
            {
                List<Tuple<CharacterType, int>> characters = _characterLocations[_characterLocations.Keys.ElementAt(0)];
                if (characters.Count == 1 && characters[0].Item1 == CharacterType.Player)
                {
                    if (Utils.Game.Level == MaxNumberOfLevel)
                        GameFinished(true);
                    else
                    {
                        GenerateGameForSpecifiedLevel(++Utils.Game.Level);
                    }
                }
            }
        }

        /// <summary>
        /// Zaktualizuj czas trwania wszystkich bonusów,
        /// </summary>
        /// <param name="gameTimeSeconds">Czas trwania gry w sekundach</param>
        private void UpdateBonusesDuration(float gameTimeSeconds)
        {
            if (!_currentFastBonusCycle.Equals(0f))
            {
                _currentFastBonusTime += gameTimeSeconds;
                if (_currentFastBonusTime >= _currentFastBonusCycle)
                {
                    //bonus prędkości przestanie działać
                    _currentFastBonusCycle = 0f;
                    _currentSlowBonusTime = 0f;
                    _currentPlayerMoveTimeCycle = PLayerMoveDurationCycle;
                }
            }
            if (!_currentSlowBonusCycle.Equals(0f))
            {
                _currentSlowBonusTime += gameTimeSeconds;
                if (_currentSlowBonusTime >= _currentSlowBonusCycle)
                {
                    //bonus spowolnienia przestanie działać
                    _currentSlowBonusCycle = 0f;
                    _currentSlowBonusTime = 0f;
                    _currentOpponentMoveTimeCycyle = OpponentMoveCycyle;
                }
            }
            if (!_currentStrengthBonusCycle.Equals(0f))
            {
                _currentStrengthBonusTime += gameTimeSeconds;
                if (_currentStrengthBonusTime >= _currentStrengthBonusCycle)
                {
                    //bonus siły przestanie działać
                    _currentStrengthBonusCycle = 0f;
                    _currentStrengthBonusTime = 0f;
                    _isSuperBomb = false;
                }
            }
            if (!_currentInmortalBonusCycle.Equals(0f))
            {
                _currentInmortalBonusTime += gameTimeSeconds;
                if (_currentInmortalBonusTime >= _currentInmortalBonusCycle)
                {
                    //bonus nieśmiertelności przestanie działać
                    _currentInmortalBonusCycle = 0f;
                    _currentInmortalBonusTime = 0f;
                    _isInmortal = false;
                }
            }
        }

        /// <summary>
        /// Zaktualizuj pozycje postaci na planszy.
        /// </summary>
        /// <param name="gameTimeSeconds">Czas trwani gry w sekundach</param>
        private void UpdateCharactersLocations(float gameTimeSeconds)
        {
            _currentOpponentMoveTime += gameTimeSeconds;
            if (_currentOpponentMoveTime >= _currentOpponentMoveTimeCycyle)
            {
                _currentOpponentMoveTime -= _currentOpponentMoveTimeCycyle;
                MoveOpponents();
            }
            // uaktulanij ruch gracza
            _currentPlayerMoveTime += gameTimeSeconds;
            if (_currentPlayerMoveTime >= _currentPlayerMoveTimeCycle)
            {
                _currentPlayerMoveTime -= _currentPlayerMoveTimeCycle;
                HandleKeyBomb();
                HandleKeyboard();
            }
        }

        /// <summary>
        /// Uaktualnij bomby, niektóre powinny zniknąc bo właśnie wybuchają.
        /// </summary>
        /// <param name="gameTimeSeconds">Czas trwani gry w sekundach</param>
        private void UpdateBombs(float gameTimeSeconds)
        {
            List<float> newCurrentTimes = new List<float>();
            for (int i = 0; i < _currentBombTimes.Count; i++)
            {
                _currentBombTimes[i] += gameTimeSeconds;
                if (_bombLocations.Count == 0)
                {
                    _currentBombTimes.Clear();
                    break;
                }
                if (_currentBombTimes[i] >= BombCycleDuration)
                {
                    LaunchBomb(_bombLocations[i]);
                    _bombLocations.RemoveAt(i);
                }
                else
                {
                    newCurrentTimes.Add(_currentBombTimes[i]);
                }
            }
            _currentBombTimes = newCurrentTimes;
        }

        /// <summary>
        /// Odpal bombę na podaje pozycji, zmień kolory wszystkich pól odpowiednio
        /// </summary>
        /// <param name="bombPosition">pozycja bomby</param>
        private void LaunchBomb(int bombPosition)
        {
            // powiedz duchom że bomba wybuchła czyli był tu gracz
            TellGhoastWhereUserWas(bombPosition);
            int row = bombPosition/_columns;
            int bombScope = _isSuperBomb ? 6 : 4;
            //zniszcz wszystkie pola w poziomie
            //idź w prawo aż nie napotkasz pola czarnego
            for (int i = 0; i < bombScope; i++)
            {
                int blockPosition = bombPosition + i;
                if (blockPosition >= _columns*_rows) break;
                if (row != blockPosition/_columns) break;
                if (_boardBlocksTypes[blockPosition] == BlockType.Black) break;
                if (_boardBlocksTypes[blockPosition] == BlockType.Grey)
                {
                    Utils.Game.Points += DeletingGreyField;
                    _boardBlocksTypes[blockPosition] = BlockType.Red;
                    break;
                }
                _boardBlocksTypes[blockPosition] = BlockType.Red;
            }
            //idź w lewo aż nie napotkasz pola czarnego
            for (int i = 0; i < bombScope; i++)
            {
                int blockPosition = bombPosition - i;
                if (blockPosition < 0) break;
                if (row != blockPosition/_columns) break;
                if (_boardBlocksTypes[blockPosition] == BlockType.Black) break;
                if (_boardBlocksTypes[blockPosition] == BlockType.Grey)
                {
                    Utils.Game.Points += DeletingGreyField;
                    _boardBlocksTypes[blockPosition] = BlockType.Red;
                    break;
                }
                _boardBlocksTypes[blockPosition] = BlockType.Red;
            }
            //idź w górę aż nie napotkasz pola czarnego
            for (int i = 0; i < bombScope; i++)
            {
                int blockPosition = bombPosition - i*_columns;
                if (blockPosition < 0) break;
                if (_boardBlocksTypes[blockPosition] == BlockType.Black) break;
                if (_boardBlocksTypes[blockPosition] == BlockType.Grey)
                {
                    Utils.Game.Points += DeletingGreyField;
                    _boardBlocksTypes[blockPosition] = BlockType.Red;
                    break;
                }
                _boardBlocksTypes[blockPosition] = BlockType.Red;
            }
            //idź w dół aż nie napotkasz pola czarnego
            for (int i = 0; i < bombScope; i++)
            {
                int blockPosition = bombPosition + i*_columns;
                if (blockPosition >= _rows*_columns) break;
                if (_boardBlocksTypes[blockPosition] == BlockType.Black) break;
                if (_boardBlocksTypes[blockPosition] == BlockType.Grey)
                {
                    Utils.Game.Points += DeletingGreyField;
                    _boardBlocksTypes[blockPosition] = BlockType.Red;
                    break;
                }
                _boardBlocksTypes[blockPosition] = BlockType.Red;
            }
        }

        /// <summary>
        /// Daje znak duchowi, gdzie wybuchła bomba
        /// </summary>
        /// <param name="bombPositionExplode">The bomb position explode.</param>
        private void TellGhoastWhereUserWas(int bombPositionExplode)
        {
            Dictionary<int, List<Tuple<CharacterType, int>>> tmpCharacterLocations = new Dictionary<int, List<Tuple<CharacterType, int>>>();
            for (int i = 0; i < _rows * _columns; i++)
            {
                if (_characterLocations.ContainsKey(i))
                {
                    List<Tuple<CharacterType, int>> characterTypes = _characterLocations[i];
                    foreach (var character in characterTypes)
                    {
                        var tmpCharacter = character;
                        if (tmpCharacter.Item1 == CharacterType.Ghost)
                        {
                            //if (tmpCharacter.Item2 == -1)
                                tmpCharacter = new Tuple<CharacterType, int>(tmpCharacter.Item1, bombPositionExplode);
                            if (!tmpCharacterLocations.ContainsKey(i))
                                tmpCharacterLocations.Add(i, new List<Tuple<CharacterType, int>>());
                            tmpCharacterLocations[i].Add(tmpCharacter);
                        }
                        else
                        {
                            if (!tmpCharacterLocations.ContainsKey(i))
                            {
                                tmpCharacterLocations.Add(i, new List<Tuple<CharacterType, int>>());
                            }
                            tmpCharacterLocations[i].Add(tmpCharacter);
                        }
                    }
                }
            }
            _characterLocations = tmpCharacterLocations;
        }

        /// <summary>
        /// Zniszcz znajdujących się na polach czerownych przeciwników lub zakończ grę.
        /// </summary>
        /// <param name="position">pozycja pola czerownego</param>
        private void DeleteCharacters(int position)
        {
            if (_characterLocations.ContainsKey(position))
            {
                List<Tuple<CharacterType,int>> characters = _characterLocations[position];
                foreach (var character in characters)
                {
                    switch (character.Item1)
                    {
                        case CharacterType.Ghost:
                            Utils.Game.Points += DeletingGhoast;
                            break;
                        case CharacterType.Octopus:
                            Utils.Game.Points += DeletingOctopus;
                            break;
                        case CharacterType.Player:
                            GameFinished(false);
                            return;
                    }
                }
                characters.RemoveAll(x => x.Item1 != CharacterType.Player);
                if (characters.Count == 0)
                {
                    _characterLocations.Remove(position);
                }
            }
        }

        /// <summary>
        /// Sprawdź czy gracz znalazł się na tym samym polu co inny przeciwnik.
        /// Jeżeli tak to zakończ grę.
        /// </summary>
        private void CheckIfPlayerIsOnSameFieldAsAnyOpponent()
        {
            if (_characterLocations.ContainsKey(_boardEngine.PlayerLocation))
            {
                List<Tuple<CharacterType, int>> characterTypes = _characterLocations[_boardEngine.PlayerLocation];
                for (int i = 0; i < characterTypes.Count; i++)
                {
                    if (characterTypes[i].Item1 != CharacterType.Player)
                    {
                        GameFinished(false);
                    }
                }
            }
        }

        /// <summary>
        /// Sprawdź czy użytkownik powinien zebrać jakiś bonus.
        /// </summary>
        private void CheckIfUserShouldGetAnyBonus()
        {
            if (_bonusLocations.ContainsKey(_boardEngine.PlayerLocation) &&
                _boardBlocksTypes[_boardEngine.PlayerLocation] == BlockType.White)
            {
                BonusType bonusType = _bonusLocations[_boardEngine.PlayerLocation];
                switch (bonusType)
                {
                    case BonusType.BombAmount:
                        _bombAmount++;
                        break;
                    case BonusType.Fast:
                        // zwiększ prędkość gracza dwukrotnie
                        _currentPlayerMoveTimeCycle = PLayerMoveDurationCycle/2;
                        // zwiększ długość trwania bonusu prędkości
                        _currentFastBonusCycle += FastBonusDuration;
                        break;
                    case BonusType.Inmortal:
                        _currentInmortalBonusCycle += InmortalBonusDuration;
                        _isInmortal = true;
                        break;
                    case BonusType.Slow:
                        _currentSlowBonusCycle += SlowBonusDuration;
                        _currentOpponentMoveTimeCycyle = OpponentMoveCycyle*3;
                        break;
                    case BonusType.Points:
                        Utils.Game.Points += PointBonus;
                        break;
                    case BonusType.Strenght:
                        _currentStrengthBonusCycle += StrengthBonusDuration;
                        _isSuperBomb = true;
                        break;
                }
                _bonusLocations.Remove(_boardEngine.PlayerLocation);
            }
        }

        /// <summary>
        /// Wykonaj ruch dla wszytskich przeciwników.
        /// </summary>
        private void MoveOpponents()
        {
            Dictionary<int, List<Tuple<CharacterType,int>>> tmpCharacterLocations = new Dictionary<int, List<Tuple<CharacterType, int>>>();
            for (int i = 0; i < _rows*_columns; i++)
            {
                if (_characterLocations.ContainsKey(i))
                {
                    List<Tuple<CharacterType,int>> characterTypes = _characterLocations[i];
                    foreach (var character in characterTypes)
                    {
                        var tmpCharacter = character;
                        if (tmpCharacter.Item1 == CharacterType.Octopus)
                        {
                            // jeżeli udało dojść się do pola, które było gonione to trzeba znaleźć nowe, więc wyrzucić to co zapisane
                            // i wpisać -1.
                            if(tmpCharacter.Item2 == i) 
                                tmpCharacter  = new Tuple<CharacterType, int>(tmpCharacter.Item1, -1);
                            int newPosition = -1;
                            int newIndex = GenerateOpponentMove(i, tmpCharacter.Item2, out newPosition);
                            if (newPosition != -1)
                            {
                                tmpCharacter = new Tuple<CharacterType, int>(tmpCharacter.Item1, newPosition);
                            }
                            if (newIndex == -1) newIndex = RandomOpponentMove(i);
                            if (!tmpCharacterLocations.ContainsKey(newIndex))
                                tmpCharacterLocations.Add(newIndex, new List<Tuple<CharacterType, int>>());
                            tmpCharacterLocations[newIndex].Add(tmpCharacter);
                        }
                        else if (tmpCharacter.Item1 == CharacterType.Ghost)
                        {
                            // jeżeli udało dojść się do pola, które było gonione to trzeba znaleźć nowe, więc wyrzucić to co zapisane
                            // i wpisać -1.
                            if (tmpCharacter.Item2 == i)
                                tmpCharacter = new Tuple<CharacterType, int>(tmpCharacter.Item1, -1);
                            int newPosition = -1;
                            int newIndex = GenerateOpponentMove(i, tmpCharacter.Item2, out newPosition);
                            if (newPosition != -1)
                            {
                                tmpCharacter = new Tuple<CharacterType, int>(tmpCharacter.Item1, newPosition);
                            }
                            if (newIndex == -1) newIndex = RandomOpponentMove(i);
                            if (!tmpCharacterLocations.ContainsKey(newIndex))
                                tmpCharacterLocations.Add(newIndex, new List<Tuple<CharacterType, int>>());
                            tmpCharacterLocations[newIndex].Add(tmpCharacter);
                        }
                        else
                        {
                            if (!tmpCharacterLocations.ContainsKey(i))
                            {
                                tmpCharacterLocations.Add(i, new List<Tuple<CharacterType, int>>());
                            }
                            tmpCharacterLocations[i].Add(tmpCharacter);
                        }
                    }
                }
            }
            _characterLocations = tmpCharacterLocations;
        }

        /// <summary>
        /// Wygeneruj ruch ośmiornicy lub ducha na podstawie pozycji gracza.
        /// </summary>
        /// <param name="opponentPosition">pozycja ośmiornicy</param>
        /// <param name="destinationFiled">pozycja do której chce się dostać postać</param>
        /// <param name="newDestinationField">zwracane <c>-1</c> jeżeli nie udało się zobaczeć gracza, jeżeli jednak widzi się gracza to wpisuje się jego pozycję</param>
        /// <returns>zwróć -1 jeżeli nie udało się wylosować poprawnej pozycji w p.p zwróć nową pozycję</returns>
        private int GenerateOpponentMove(int opponentPosition, int destinationFiled, out int newDestinationField)
        {
            int row = opponentPosition/_columns;
            int column = opponentPosition - row*_columns;
            int playerRow = _boardEngine.PlayerLocation/_columns;
            int playerColumn = _boardEngine.PlayerLocation - playerRow*_columns;

            // nie rozpatrzamy przypadku kiedy ośmiornica nachodzi na gracza bo wcześniej to wygeneruje zakończenie gry
            if (row == playerRow)
            {
                bool see = true;
                //sprawdź czy przeciwnik widzi gracza
                for (int i = row*_columns + Math.Min(column, playerColumn) + 1;
                    i < row*_columns + Math.Max(column, playerColumn);
                    i++)
                {
                    if (_boardBlocksTypes[i] == BlockType.Black || _boardBlocksTypes[i] == BlockType.Grey)
                    {
                        see = false;
                        break;
                    }
                }
                if (see)
                {
                    //idź w lewo do gracza
                    if (playerColumn < column)
                    {
                        if (_boardBlocksTypes[opponentPosition - 1] != BlockType.Red)
                        {
                            newDestinationField = _boardEngine.PlayerLocation;
                            return opponentPosition - 1;
                        }
                    }
                    // idź w prawo do gracza
                    if (playerColumn > column)
                    {
                        if (_boardBlocksTypes[opponentPosition + 1] != BlockType.Red)
                        {
                            newDestinationField = _boardEngine.PlayerLocation;
                            return opponentPosition + 1;
                        }
                    }
                }
            }
            else if (column == playerColumn)
            {
                bool see = true;
                //sprawdź czy przeciwnik widzi gracza
                for (int i = (Math.Min(row, playerRow) + 1)*_columns + column;
                    i < (Math.Max(row, playerRow))*_columns + column;
                    i += _columns)
                {
                    if (_boardBlocksTypes[i] == BlockType.Black || _boardBlocksTypes[i] == BlockType.Grey)
                    {
                        see = false;
                        break;
                    }
                }
                if (see)
                {
                    //idź w górę do gracza
                    if (playerRow < row)
                    {
                        if (_boardBlocksTypes[opponentPosition - _columns] != BlockType.Red)
                        {
                            newDestinationField = _boardEngine.PlayerLocation;
                            return opponentPosition - +_columns;
                        }
                    }
                    // idź w dół do gracza
                    if (playerRow > row)
                    {
                        if (_boardBlocksTypes[opponentPosition + _columns] != BlockType.Red)
                        {
                            newDestinationField = _boardEngine.PlayerLocation;
                            return opponentPosition + +_columns;
                        }
                    }
                }
            }
            // nie widać gracza
            newDestinationField = -1;
            // sprawdź czy nie ma nic wpisanego w pole w parze
            if (destinationFiled != -1)
            {
                int position;
                //int destRow = destinationFiled / _columns;
                //int destColumn = destinationFiled - destRow * _columns;
                //// idź jedno pole w prawo
                //if (destColumn > column && _boardBlocksTypes[opponentPosition + 1] == BlockType.White)
                //{
                //    return opponentPosition + 1;
                //}
                ////idź jedo pole w górę
                //if (destRow < row && _boardBlocksTypes[opponentPosition - _columns] == BlockType.White)
                //{
                //    return opponentPosition - _columns;
                //}
                ////idź jedo pole w lewo
                //if (destColumn < column && _boardBlocksTypes[opponentPosition + 1] == BlockType.White)
                //{
                //    return opponentPosition - 1;
                //}
                ////idź jedo pole w dół
                //if (row < destRow && _boardBlocksTypes[opponentPosition + _columns] == BlockType.White)
                //{
                //    return opponentPosition + _columns;
                //}
                //// spróbuj iśc w kieruku pola
                if (FindShortestPathBeetweenTwoPoints(opponentPosition, destinationFiled, out position))
                {
                    if (position != opponentPosition && _boardBlocksTypes[position] != BlockType.Red)
                        return position;
                }
            }
            return -1;
        }

        /// <summary>
        /// Znajdź najkrótszą ścieżkę między dwoma punktami o ile istnieje
        /// </summary>
        /// <param name="start">pozycja startowa ducha</param>
        /// <param name="end">poycja docelowa</param>
        /// <param name="nextPosition">pozycja pola nastęnego po start gdzie powinien udać się duch</param>
        /// <returns>zwróć <c>true</c> jak znaleziono ścieżkę</returns>
        private bool FindShortestPathBeetweenTwoPoints(int start, int end, out int nextPosition)
        {
            // zapuść algorytm szukający ścieżki
            bool[] visited = new bool[_boardBlocksTypes.Count];
            int max = _boardBlocksTypes.Count(field => field == BlockType.White || field == BlockType.Grey);
            bool ifPahExists = false;
            int[] bestPath = null;
            FindPath(visited, 0, start, ref max, end, ref ifPahExists, new int[max+1], ref bestPath );
            if (ifPahExists)
            {
                nextPosition = bestPath[1];
                return true;
            }
            // nie idź od start do end
            nextPosition = -1;
            return false;
        }

        /// <summary>
        /// Pomocnicza funkjca wywoływana rekurencyjnie aby sprawdzić czy można dojść z
        /// dowolnego pola planszy (nie czarnego) do dowolnego pola planszy (nie czarnego)
        /// </summary>
        /// <param name="visited">pola odwiedzone</param>
        /// <param name="index">index wierchołka, na którym jesteśmy</param>
        /// <param name="vertexes">ilośc odwiedzonych wierzchołków</param>
        /// <param name="max">maxilość odwiedzonych wierzchołków</param>
        /// <param name="end">końcowy wierzchołek</param>
        /// <param name="gained">czy osiągnięto cel</param>
        /// <param name="moves">ścieżka poruszania się</param>
        /// <param name="bestMoves">najlepsza ścieżka</param>
        private void FindPath(bool[] visited, int vertexes,
            int index, ref int max, int end, ref bool gained, int[] moves, ref int[] bestMoves)
        {
            if (vertexes == max) return;
            if (index == end)
            {
                gained = true;
                if (vertexes == Math.Min(vertexes, max))
                {
                    bestMoves = new int[vertexes+1];
                    for (int i = 0; i < vertexes; i++)
                    {
                        bestMoves[i] = moves[i];
                    }
                    bestMoves[vertexes] = end;
                }
                max = Math.Min(vertexes, max);
                return;
            }
            if (index < 0) return;
            if (index >= visited.Length) return;
            if (_boardBlocksTypes[index] == BlockType.Black || _boardBlocksTypes[index] == BlockType.Grey) return;
            if (visited[index]) return;
            // zwiększamy ilośc odwiedzonych wierzchołków
            if (!visited[index])
            {
                visited[index] = true;
                moves[vertexes] = index;
                vertexes++;
            }
            if ((index + 1) % _columns != 0) FindPath(visited, vertexes, index + 1, ref max, end, ref  gained, moves, ref bestMoves);
            if (index - _columns >= 0) FindPath(visited, vertexes, index - _columns, ref max, end, ref gained, moves, ref bestMoves);
            if (index % _columns != 0) FindPath(visited, vertexes, index - 1, ref max, end, ref gained, moves, ref bestMoves);
            if (index + _columns < visited.Length)
                FindPath(visited, vertexes, index + _columns, ref max, end, ref gained, moves, ref bestMoves);
            visited[index] = false;
        }

        /// <summary>
        /// Wylosuj ruch przeciwnika.
        /// </summary>
        /// <param name="opponentPosition">Pozycja przeciwnika</param>
        /// <returns></returns>
        private int RandomOpponentMove(int opponentPosition)
        {
            List<int> indexes = new List<int>();
            int row = opponentPosition/_columns;
            int column = opponentPosition - row*_columns;
            // jeśli można iść w prawo
            if (column != _columns - 1)
            {
                if (_boardBlocksTypes[opponentPosition + 1] == BlockType.White)
                    indexes.Add(opponentPosition + 1);
            }
            // można iść w lewo
            if (column != 0)
            {
                if (_boardBlocksTypes[opponentPosition - 1] == BlockType.White)
                    indexes.Add(opponentPosition - 1);
            }
            // można iść do góry
            if (row != 0)
            {
                if (_boardBlocksTypes[opponentPosition - _columns] == BlockType.White)
                    indexes.Add(opponentPosition - _columns);
            }
            // można iść w dół
            if (row != _rows - 1)
            {
                if (_boardBlocksTypes[opponentPosition + _columns] == BlockType.White)
                    indexes.Add(opponentPosition + _columns);
            }
            if (indexes.Count == 0) return opponentPosition;
            int index = _random.Next(indexes.Count);
            return indexes[index];
        }

        #endregion

        /// <summary>
        /// Zakończ grę w zależności od wyniku gry.
        /// </summary>
        /// <param name="win">Jeżeli wartość <c>true</c> zakończ grę sukcesem. W p.p. komuniat o przegranej. Zapisz stan gry do bazy danych.</param>
        private void GameFinished(bool win)
        {
            if (!win)
            {
                if (_isInmortal)
                {
                    return;
                }
                _informationLabel.Text = TryAgain;
                _shouldUpdate = false;
            }
            else
            {
                if (Utils.Game.Level == MaxNumberOfLevel)
                {
                    _informationLabel.Text = BravoWinning;
                    _shouldUpdate = false;
                }
            }
            Utils.Game.Finished = true;
            SaveGame();
        }

        #region KeyboardHandle

        /// <summary>
        /// Obsłuż wciśnięty klawisz odpowiedzialny za stawiane bomby.
        /// Obsługa przytrzymanego klawisza.
        /// </summary>
        private void HandleKeyBomb()
        {
            KeyboardState = Keyboard.GetState();
            if (LastKeyboardState == KeyboardState) return;
            Keys[] keymap = KeyboardState.GetPressedKeys();
            int gamer = _boardEngine.PlayerLocation;
            if (_bombLocations.Count < _bombAmount)
            {
                if (Utils.User.BombKeyboardOption.Equals(BombKeyboardOption.Spcace))
                {
                    if (keymap.Contains(Keys.Space))
                    {
                        _bombLocations.Add(gamer);
                        _currentBombTimes.Add(0);
                    }
                }
                else
                {
                    if (keymap.Contains(Keys.P))
                    {
                        _bombLocations.Add(gamer);
                        _currentBombTimes.Add(0);
                    }
                }
            }
            LastKeyboardState = KeyboardState;
        }

        /// <summary>
        /// Obsłuż wciskane klawisze na klawiaturze w zależności od wybranej opcji.
        /// </summary>
        public override void HandleKeyboard()
        {
            KeyboardState = Keyboard.GetState();
            Keys[] keymap = KeyboardState.GetPressedKeys();
            int playerLocation = _boardEngine.PlayerLocation;
            int playerRow = playerLocation/_columns;
            int playerColumn = playerLocation - playerRow*_columns;
            foreach (Keys k in keymap)
            {
                switch (k)
                {
                    case Keys.Down:
                        if (Utils.User.KeyboardOption.Equals(KeyboardOption.Arrows))
                            HandleKeyDown(playerRow, playerColumn, playerLocation);
                        break;
                    case Keys.S:
                        if (Utils.User.KeyboardOption.Equals(KeyboardOption.Wsad))
                            HandleKeyDown(playerRow, playerColumn, playerLocation);
                        break;
                    case Keys.Up:
                        if (Utils.User.KeyboardOption.Equals(KeyboardOption.Arrows))
                            HandleKeyUp(playerRow, playerColumn, playerLocation);
                        break;
                    case Keys.W:
                        if (Utils.User.KeyboardOption.Equals(KeyboardOption.Wsad))
                            HandleKeyUp(playerRow, playerColumn, playerLocation);
                        break;
                    case Keys.Left:
                        if (Utils.User.KeyboardOption.Equals(KeyboardOption.Arrows))
                            HandleKeyLeft(playerRow, playerColumn, playerLocation);
                        break;
                    case Keys.A:
                        if (Utils.User.KeyboardOption.Equals(KeyboardOption.Wsad))
                            HandleKeyLeft(playerRow, playerColumn, playerLocation);
                        break;
                    case Keys.Right:
                        if (Utils.User.KeyboardOption.Equals(KeyboardOption.Arrows))
                            HandleKeyRight(playerRow, playerColumn, playerLocation);
                        break;
                    case Keys.D:
                        if (Utils.User.KeyboardOption.Equals(KeyboardOption.Wsad))
                            HandleKeyRight(playerRow, playerColumn, playerLocation);
                        break;
                    case Keys.Back:
                    case Keys.Escape:
                    case Keys.Home:
                        _backButton.Click();
                        break;
                }
            }
            LastKeyboardState = KeyboardState;
        }

        /// <summary>
        /// Obsługa pójścia w dół przez gracza.
        /// </summary>
        /// <param name="playerRow">wiersz pozycji gracza</param>
        /// <param name="playerColumn">kolumna pozycji gracza</param>
        /// <param name="playerLocation">lokalizacja gracza na planszy</param>
        private void HandleKeyDown(int playerRow, int playerColumn, int playerLocation)
        {
            if (playerRow < _rows - 1)
            {
                int tmpLocation = (playerRow + 1)*_columns + playerColumn;
                if (_boardBlocksTypes[tmpLocation].Equals(BlockType.White) ||
                    _boardBlocksTypes[tmpLocation].Equals(BlockType.Red))
                {
                    if (!_characterLocations.ContainsKey(playerLocation)) return;
                    _characterLocations[playerLocation].RemoveAll(x => x.Item1 == CharacterType.Player);
                    if (_characterLocations[playerLocation].Count == 0) _characterLocations.Remove(playerLocation);
                    playerRow++;
                    playerLocation = playerRow*_columns + playerColumn;
                    if (_characterLocations.ContainsKey(playerLocation))
                        _characterLocations[playerLocation].Add(new Tuple<CharacterType, int>(CharacterType.Player, -1));
                    else
                    {
                        _characterLocations.Add(playerLocation, new List<Tuple<CharacterType,int>>()
                        {
                            new Tuple<CharacterType, int>(CharacterType.Player, -1)
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Obsługa pójścia w górę przez gracza.
        /// </summary>
        /// <param name="playerRow">wiersz pozycji gracza</param>
        /// <param name="playerColumn">kolumna pozycji gracza</param>
        /// <param name="playerLocation">lokalizacja gracza na planszy</param>
        private void HandleKeyUp(int playerRow, int playerColumn, int playerLocation)
        {
            if (playerRow > 0)
            {
                int tmp = (playerRow - 1)*_columns + playerColumn;
                if (_boardBlocksTypes[tmp].Equals(BlockType.White) ||
                    _boardBlocksTypes[tmp].Equals(BlockType.Red))
                {
                    if (!_characterLocations.ContainsKey(playerLocation)) return;
                    _characterLocations[playerLocation].RemoveAll(x => x.Item1 == CharacterType.Player);
                    if (_characterLocations[playerLocation].Count == 0) _characterLocations.Remove(playerLocation);
                    playerRow--;
                    playerLocation = playerRow*_columns + playerColumn;
                    if (_characterLocations.ContainsKey(playerLocation))
                        _characterLocations[playerLocation].Add(new Tuple<CharacterType, int>(CharacterType.Player, -1));
                    else
                    {
                        _characterLocations.Add(playerLocation, new List<Tuple<CharacterType, int>>()
                        {
                            new Tuple<CharacterType, int>(CharacterType.Player, -1)
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Obsługa pójścia w prawo przez gracza.
        /// </summary>
        /// <param name="playerRow">wiersz pozycji gracza</param>
        /// <param name="playerColumn">kolumna pozycji gracza</param>
        /// <param name="playerLocation">lokalizacja gracza na planszy</param>
        private void HandleKeyRight(int playerRow, int playerColumn, int playerLocation)
        {
            if (playerColumn < _columns - 1)
            {
                int tmp = playerRow*_columns + playerColumn + 1;
                if (_boardBlocksTypes[tmp].Equals(BlockType.White) ||
                    _boardBlocksTypes[tmp].Equals(BlockType.Red))
                {
                    if (!_characterLocations.ContainsKey(playerLocation)) return;
                    _characterLocations[playerLocation].RemoveAll(x => x.Item1 == CharacterType.Player);
                    if (_characterLocations[playerLocation].Count == 0) _characterLocations.Remove(playerLocation);
                    playerColumn++;
                    playerLocation = playerRow*_columns + playerColumn;
                    if (_characterLocations.ContainsKey(playerLocation))
                        _characterLocations[playerLocation].Add(new Tuple<CharacterType, int>(CharacterType.Player, -1));
                    else
                    {
                        _characterLocations.Add(playerLocation, new List<Tuple<CharacterType, int>>()
                        {
                            new Tuple<CharacterType, int>(CharacterType.Player, -1)
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Obsługa pójścia w lewo przez gracza.
        /// </summary>
        /// <param name="playerRow">wiersz pozycji gracza</param>
        /// <param name="playerColumn">kolumna pozycji gracza</param>
        /// <param name="playerLocation">lokalizacja gracza na planszy</param>
        private void HandleKeyLeft(int playerRow, int playerColumn, int playerLocation)
        {
            if (playerColumn > 0)
            {
                int tmp = playerRow*_columns + playerColumn - 1;
                if (_boardBlocksTypes[tmp].Equals(BlockType.White) ||
                    _boardBlocksTypes[tmp].Equals(BlockType.Red))
                {
                    if (!_characterLocations.ContainsKey(playerLocation)) return;
                    _characterLocations[playerLocation].RemoveAll(x => x.Item1 == CharacterType.Player);
                    if (_characterLocations[playerLocation].Count == 0) _characterLocations.Remove(playerLocation);
                    playerColumn--;
                    playerLocation = playerRow*_columns + playerColumn;
                    if (_characterLocations.ContainsKey(playerLocation))
                        _characterLocations[playerLocation].Add(new Tuple<CharacterType, int>(CharacterType.Player, -1));
                    else
                    {
                        _characterLocations.Add(playerLocation, new List<Tuple<CharacterType, int>>()
                        {
                            new Tuple<CharacterType, int>(CharacterType.Player, -1)
                        });
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Utwórz nową instancję gry z poziomem 0, rozpoczynającym grę
        /// </summary>
        /// <returns>Zwróć instancję GameDAO i operuj na niej do zakończenia jednej gry</returns>
        public GameDao CreateNewGame()
        {
            GenerateGameForSpecifiedLevel(0);
            GameDao gameDao = new GameDao()
            {
                Id = -1,
                Level = 0,
                Finished = false,
                PlayerXLocation =  (_boardEngine.PlayerLocation/_columns),
                PlayerYLocation =  (_boardEngine.PlayerLocation - _boardEngine.PlayerLocation/_columns*_columns),
                Points = 0,
                SaveTime = DateTime.Now,
                User = Utils.User
            };
            return gameDao;
        }

        /// <summary>
        /// Utwórz nowy BoadEngine potrzebny do zarządzania polami planszy
        /// </summary>
        /// <param name="rows">ilość jednoskowych pól w jednej kolumnie</param>
        /// <param name="columns">ilość jednostkowych pól w jednym wierszu</param>
        private void CreateBoardEngine(int rows, int columns)
        {
            _boardEngine = new BoardEngine(_blockTextures, _bonusesTextures, _characterTextures,
                _bombTexture, rows, columns);
            _rows = rows;
            _columns = columns;
        }

        #region save

        /// <summary>
        /// Zapisz dotychczasowy stan gry do bazie danych
        /// </summary>
        private void SaveGame()
        {
            bool prevState = _shouldUpdate;
            _shouldUpdate = false;
            String message;
            int x = _boardEngine.PlayerLocation/_columns;
            int y = _boardEngine.PlayerLocation - x*_columns;
            Utils.Game.PlayerXLocation = x;
            Utils.Game.PlayerYLocation = y;
            Utils.Game.BombsAmount = _bombAmount;
            bool savedGame = false;
            Utils.Game.User = Utils.User;
            if (Utils.Game.Id == -1)
            {
                if (GameService.CreateGame(ref Utils.Game, out message))
                {
                    savedGame = true;
                }
            }
            else
            {
                if (GameService.UpdateGame(ref Utils.Game, out message))
                {
                    savedGame = true;
                }
            }
            if (savedGame)
            {
                List<OpponentLocationDao> opponentLocations = new List<OpponentLocationDao>();
                List<BoardElementLocationDao> boardElementLocation = new List<BoardElementLocationDao>();
                SaveBlocksAndOpponents(ref opponentLocations, ref boardElementLocation, out message);
                SaveBombs(ref boardElementLocation, out message);
                SaveBonuses(ref boardElementLocation, out message);
                OpponentService.UpdateOpponentLocations(Utils.Game.Id, opponentLocations, out message);
                BoardService.UpdateBoardElementLocations(Utils.Game.Id, boardElementLocation, out message);
            }
            _shouldUpdate = prevState;
        }

        /// <summary>
        /// Zapisz wszytskie bonusy wraz ich pozostałymi czasami życia. Bonus, które są już w trakcie kożystania
        /// zapisywane są na polu -1,-1 to oznacza że po wczytaniu gry z tego pola wpisywane będą wartości wszystkich niewykorzystanych do końca bonusów.
        /// </summary>
        /// <param name="boardElementLocation">rozmieszczenie jednostkowych pól</param>
        /// <param name="message">w razie błędów komunikat o problemach</param>
        private void SaveBonuses(ref List<BoardElementLocationDao> boardElementLocation, out String message)
        {
            message = null;
            for (int i = 0; i < _bonusLocations.Keys.Count; i++)
            {
                BoardElementType boardElementType = BoardElementType.BombAmountBonus;
                switch (_bonusLocations[_bonusLocations.Keys.ElementAt(i)])
                {
                    case BonusType.BombAmount:
                        boardElementType = BoardElementType.BombAmountBonus;
                        break;
                    case BonusType.Fast:
                        boardElementType = BoardElementType.FastBonus;
                        break;
                    case BonusType.Inmortal:
                        boardElementType = BoardElementType.InmortalBonus;
                        break;
                    case BonusType.Slow:
                        boardElementType = BoardElementType.SlowBonus;
                        break;
                    case BonusType.Points:
                        boardElementType = BoardElementType.PointBonus;
                        break;
                    case BonusType.Strenght:
                        break;
                }
                BoardElementDao boardElementDao = BoardService.FindBoardElementByType(boardElementType, out message);
                BoardElementLocationDao boardElementLocationDao = new BoardElementLocationDao()
                {
                    Game = Utils.Game,
                    BoardElement = boardElementDao,
                    Timeout = null,
                    XLocation = (_bonusLocations.Keys.ElementAt(i)/_columns),
                    YLocation =
                        (_bonusLocations.Keys.ElementAt(i) - _bonusLocations.Keys.ElementAt(i)/_columns*_columns),
                };
                boardElementLocation.Add(boardElementLocationDao);
            }

            if (_currentFastBonusCycle - _currentFastBonusTime > 0f)
            {
                BoardElementDao boardElementDao = BoardService.FindBoardElementByType(BoardElementType.FastBonus,
                    out message);
                BoardElementLocationDao boardElementLocationDao = new BoardElementLocationDao()
                {
                    Game = Utils.Game,
                    BoardElement = boardElementDao,
                    Timeout = _currentFastBonusCycle - _currentFastBonusTime,
                    XLocation = -1,
                    YLocation = -1,
                };
                boardElementLocation.Add(boardElementLocationDao);
            }
            if (_currentStrengthBonusCycle - _currentStrengthBonusTime > 0f)
            {
                BoardElementDao boardElementDao = BoardService.FindBoardElementByType(BoardElementType.StrenghtBonus,
                    out message);
                BoardElementLocationDao boardElementLocationDao = new BoardElementLocationDao()
                {
                    Game = Utils.Game,
                    BoardElement = boardElementDao,
                    Timeout = _currentStrengthBonusCycle - _currentStrengthBonusTime,
                    XLocation = -1,
                    YLocation = -1,
                };
                boardElementLocation.Add(boardElementLocationDao);
            }
            if (_currentSlowBonusCycle - _currentSlowBonusTime > 0f)
            {
                BoardElementDao boardElementDao = BoardService.FindBoardElementByType(BoardElementType.SlowBonus,
                    out message);
                BoardElementLocationDao boardElementLocationDao = new BoardElementLocationDao()
                {
                    Game = Utils.Game,
                    BoardElement = boardElementDao,
                    Timeout = _currentSlowBonusCycle - _currentSlowBonusTime,
                    XLocation = -1,
                    YLocation = -1,
                };
                boardElementLocation.Add(boardElementLocationDao);
            }
            if (_currentInmortalBonusCycle - _currentInmortalBonusTime > 0f)
            {
                BoardElementDao boardElementDao = BoardService.FindBoardElementByType(BoardElementType.InmortalBonus,
                    out message);
                BoardElementLocationDao boardElementLocationDao = new BoardElementLocationDao()
                {
                    Game = Utils.Game,
                    BoardElement = boardElementDao,
                    Timeout = _currentInmortalBonusCycle - _currentInmortalBonusTime,
                    XLocation = -1,
                    YLocation = -1,
                };
                boardElementLocation.Add(boardElementLocationDao);
            }
        }


        /// <summary>
        /// Zapisz wszytskich przeciwników i jednostkowe pola.
        /// </summary>
        /// <param name="opponentLocations">lokalizacja przeciwników</param>
        /// <param name="boardElementLocation">rozmieszczenie jednostkowych pól</param>
        /// <param name="message">w razie błędów komunikat o problemach</param>
        private void SaveBlocksAndOpponents(ref List<OpponentLocationDao> opponentLocations,
            ref List<BoardElementLocationDao> boardElementLocation, out String message)
        {
            message = null;
            for (int i = 0; i < _boardBlocksTypes.Count; i++)
            {
                if (_characterLocations.ContainsKey(i))
                {
                    List<Tuple<CharacterType,int>> characters = _characterLocations[i];
                    foreach (var c in characters)
                    {
                        if (c.Item1 != CharacterType.Player)
                        {
                            OpponentType opponent = OpponentType.Ghost;
                            if (c.Item1 == CharacterType.Octopus) opponent = OpponentType.Octopus;
                            OpponentDao opponentDao = OpponentService.FindBoardElementByType(opponent, out message);
                            opponentLocations.Add(new OpponentLocationDao()
                            {
                                Game = Utils.Game,
                                Oponent = opponentDao,
                                XLocation = (uint) (i/_columns),
                                YLocation = (uint) (i - i/_columns*_columns),
                            });
                        }
                    }
                }
                BoardElementType boardElementType = BoardElementType.WhiteBlock;
                float? time = null;
                switch (_boardBlocksTypes[i])
                {
                    case BlockType.Black:
                        boardElementType = BoardElementType.BlackBlock;
                        break;
                    case BlockType.Grey:
                        boardElementType = BoardElementType.GrayBlock;
                        break;
                    case BlockType.Red:
                        time = RedBlockCycleDuration - _currentRedBlockTime;
                        if (time > 0f)
                            boardElementType = BoardElementType.RedBlock;
                        else
                        {
                            time = null;
                            boardElementType = BoardElementType.WhiteBlock;
                        }
                        break;
                    case BlockType.White:
                        boardElementType = BoardElementType.WhiteBlock;
                        break;
                }
                BoardElementDao boardElementDao = BoardService.FindBoardElementByType(boardElementType, out message);
                BoardElementLocationDao boardElementLocationDao = new BoardElementLocationDao()
                {
                    Game = Utils.Game,
                    BoardElement = boardElementDao,
                    Timeout = time,
                    XLocation = (i/_columns),
                    YLocation = (i - i/_columns*_columns),
                };
                boardElementLocation.Add(boardElementLocationDao);
            }
        }

        /// <summary>
        /// Zapisz wszytskie bomby wraz ich pozostałymi czasami życia.
        /// </summary>
        /// <param name="boardElementLocation">rozmieszczenie jednostkowych pól</param>
        /// <param name="message">w razie błędów komunikat o problemach</param>
        private void SaveBombs(ref List<BoardElementLocationDao> boardElementLocation, out String message)
        {
            message = null;
            for (int i = 0; i < _bombLocations.Count; i++)
            {
                BoardElementDao boardElementDao = BoardService.FindBoardElementByType(BoardElementType.Bomb, out message);
                float timeLeft = BombCycleDuration - _currentBombTimes[i];
                if (timeLeft > 0f)
                {
                    BoardElementLocationDao boardElementLocationDao = new BoardElementLocationDao()
                    {
                        Game = Utils.Game,
                        BoardElement = boardElementDao,
                        Timeout = timeLeft,
                        XLocation = (_bombLocations[i]/_columns),
                        YLocation = (_bombLocations[i] - _bombLocations[i]/_columns*_columns),
                    };
                    boardElementLocation.Add(boardElementLocationDao);
                }
            }
        }

        #endregion

        /// <summary>
        /// Utwórz wszytskie potrzebne informacje wymagane do wyświetlenia planszy, bonusów, przeciników oraz gracza
        /// poziomy [0,4] plansza ma wyniary <value>SIMPLE</value>
        /// poziomy [5,9] plansza ma wymiary <value>MEDIUM</value>
        /// poziomy [10,14] plansza ma wymiary <value>HIGH</value>
        /// poziomy [15,19] plansza ma wymiary <value>SUPER</value>
        /// </summary>
        /// <param name="level">poziom, dla którego generowana jest plansza</param>
        private void GenerateGameForSpecifiedLevel(int level)
        {
            if (Utils.Game != null)
            {
                Utils.Game.Level = level;
                Utils.Game.Finished = false;
                Utils.Game.BombsAmount = StartBombAmount;
            }
            _informationLabel.Text = "";
            _shouldUpdate = false;
            _boardBlocksTypes = new List<BlockType>();
            _bombLocations = new List<int>();
            _currentPlayerMoveTimeCycle = PLayerMoveDurationCycle;
            _currentOpponentMoveTimeCycyle = OpponentMoveCycyle;
            _currentBombTimes = new List<float>();
            _currentFastBonusTime = 0f;
            _currentFastBonusCycle = 0f;
            _currentOpponentMoveTime = 0f;
            _currentPlayerMoveTime = 0f;
            _currentSlowBonusCycle = 0f;
            _currentSlowBonusTime = 0f;
            _currentStrengthBonusCycle = 0f;
            _currentStrengthBonusTime = 0f;
            _currentInmortalBonusCycle = StartInmortality;
            _isInmortal = true;
            _isSuperBomb = false;
            _currentInmortalBonusTime = 0f;
            _currentRedBlockTime = 0f;
            _bombAmount = StartBombAmount;
            _bonusLocations = new Dictionary<int, BonusType>();
            _characterLocations = new Dictionary<int, List<Tuple<CharacterType,int>>>();
            if (level == 0 && Utils.Game != null) Utils.Game.Points = 0;
            if (level < 0 || level > MaxNumberOfLevel)
                throw new NotImplementedException("Level Should be between indexes 0 and " + MaxNumberOfLevel);
            if (level < 5)
            {
                RandomBlocks(SimpleLevelRows, SimpleLevelColumns);
                RandomCharacters(SimpleLevelColumns);
            }
            else if (level < 10)
            {
                RandomBlocks(MediumLevelRows, MediumLevelComulns);
                RandomCharacters(MediumLevelComulns);
            }
            else if (level < 15)
            {
                RandomBlocks(HighLevelRows, HighLevelColumns);
                RandomCharacters(HighLevelColumns);
            }
            else
            {
                RandomBlocks(SuperLevelRows, SuperLevelColumns);
                RandomCharacters(SuperLevelColumns);
            }
            RandomBonuses();
            _shouldUpdate = true;
        }

        #region RandomBoardValues

        /// <summary>
        /// Sprawdza czy gracz może dojść do każdego pola nie czarnego i na nie wejść.
        /// Wystarczy sprawdzić czy z dowolnego nie czarnego pola można dojść do wszystkich nie czarnych pól.
        /// </summary>
        /// <returns></returns>
        private bool CheckIfBoardIsNiceGenerated(int blackBlockAmount, int colummns)
        {
            int verticles = 0;
            bool[] visited = new bool[_boardBlocksTypes.Count];
            int start = 0;
            while (_boardBlocksTypes[start] == BlockType.Black)
            {
                start ++;
            }
            int max = visited.Length - blackBlockAmount;
            WalkOnBoard(ref visited, ref verticles, start, max, colummns);
            if (verticles == max) return true;
            return false;
        }

        /// <summary>
        /// Pomocnicza funkjca wywoływana rekurencyjnie aby sprawdzić czy można dojść z
        /// dowolnego pola planszy (nie czarnego) do dowolnego pola planszy (nie czarnego)
        /// </summary>
        /// <param name="visited">pola odwiedzone</param>
        /// <param name="verticles">ilość wierchołków, które można odiwedzić</param>
        /// <param name="index">index wierchołka, na którym jesteśmy</param>
        /// <param name="max">oczekiwana ilość odwiedzonych wierzchołków</param>
        /// <param name="columns">ilość kolumn w jednym wierszu planszy</param>
        private void WalkOnBoard(ref bool[] visited, ref int verticles, int index, int max, int columns)
        {
            if (index < 0) return;
            if (index >= visited.Length) return;
            if (_boardBlocksTypes[index] == BlockType.Black) return;
            if (verticles == max) return;
            if (visited[index]) return;
            // zwiększamy ilośc odwiedzonych wierzchołków
            if (!visited[index])
            {
                visited[index] = true;
                verticles++;
            }
            if ((index + 1)%columns != 0) WalkOnBoard(ref visited, ref verticles, index + 1, max, columns);
            if (index%columns != 0) WalkOnBoard(ref visited, ref verticles, index - 1, max, columns);
            if (index - columns >= 0) WalkOnBoard(ref visited, ref verticles, index - columns, max, columns);
            if (index + columns < visited.Length)
                WalkOnBoard(ref visited, ref verticles, index + columns, max, columns);
        }

        /// <summary>
        /// Wylosuj pola, które powinny być niezniszczalne lub zniszczalne, pozostałe ustaw na białe, zwykłe
        /// Pola ustawiane jedynie na wartości <value>GREY</value>, <value>BLACK</value>, <value>WHITE</value>
        /// Do wszystkich pól white/grey da się dojść
        /// </summary>
        /// <param name="rows">ilość wierszy pól jednostkowch na planszy</param>
        /// <param name="columns">ilość kolumn pól jednostkowych na planszy</param>
        private void RandomBlocks(int rows, int columns)
        {
            int percentage = PercentageOfSolidBlocks*rows*columns;
            int blackBlocks = percentage%100 == 0 ? percentage/100 : percentage/100 + 1;
            CreateBoardEngine(rows, columns);

            int randomBlocks = PercentageOfSolidBlocks*rows*columns/100;
            do
            {
                _boardBlocksTypes = new List<BlockType>();
                // zapełnij całą listę szarymi zniszczalnymi blokami
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        _boardBlocksTypes.Add(BlockType.White);
                    }
                }
                // wylosuj pozycje na których ma znajdować się czarny block
                for (int i = 0; i < randomBlocks; i++)
                {
                    int x, y;
                    do
                    {
                        x = _random.Next(rows);
                        y = _random.Next(columns);
                    } while (_boardBlocksTypes[x*columns + y].Equals(BlockType.Black));
                    _boardBlocksTypes[x*columns + y] = BlockType.Black;
                }
            } while (!CheckIfBoardIsNiceGenerated(blackBlocks, columns));
            // sprawdź czy da się dojść do każdego białego bloku z dowolnego miejsca planszy

            randomBlocks = PercentageOfGreyBlocks*rows*columns/100;
            // wylosuj pozycje na których ma znajdować się szary block
            for (int i = 0; i < randomBlocks; i++)
            {
                int x, y;
                do
                {
                    x = _random.Next(rows);
                    y = _random.Next(columns);
                } while (_boardBlocksTypes[x*columns + y].Equals(BlockType.Grey) ||
                         _boardBlocksTypes[x*columns + y].Equals(BlockType.Black));
                _boardBlocksTypes[x*columns + y] = BlockType.Grey;
            }
        }

        /// <summary>
        /// Wylosuj pola, na których powinny znaleźć się bonusy
        /// Każdy typ bonusa losuj z częstotliwością w zależności od poziomu
        /// Bonusy tworzone są tylko na polach szarych, na każdym polu szarym wsytępuje maksymalnie jeden bonus.
        /// Ilość wszystkich bonusów na planszy po rozpoczęsciu poziomu to <value>PercentageOfBonuses</value> * ilość pól
        /// Wylosuj z prawdopodobieństwem 1/10 Bonus Inmortal, 3/10 Points, 2/10 Fast, 1/10 Slow, 2/10 Strength, 1/10 Extra Bomb
        /// </summary>
        private void RandomBonuses()
        {
            int maxBonusesAmount = PercentageOfBonuses*_boardBlocksTypes.Count/100 - 1;
            int counter = 0;
            while (counter < maxBonusesAmount)
            {
                int index = _random.Next(_boardBlocksTypes.Count);
                if (_boardBlocksTypes[index].Equals(BlockType.Grey) && !_bonusLocations.ContainsKey(index))
                {
                    int number = _random.Next()%10;
                    BonusType bonusType;
                    if (number == 0)
                    {
                        bonusType = BonusType.Inmortal;
                    }
                    else if (number > 0 && number < 4)
                    {
                        bonusType = BonusType.Points;
                    }
                    else if (number == 4 || number == 5)
                    {
                        bonusType = BonusType.Fast;
                    }
                    else if (number == 6)
                    {
                        bonusType = BonusType.Slow;
                    }
                    else if (number == 7 || number == 8)
                    {
                        bonusType = BonusType.Strenght;
                    }
                    else
                    {
                        bonusType = BonusType.BombAmount;
                    }
                    _bonusLocations.Add(index, bonusType);
                    counter++;
                }
            }
        }

        /// <summary>
        /// Wylosuj pola, na których powinny znaleźć się postacie przeciwników i gracz.
        /// Ilość przeciwników zależy od poziomu i jest równa <value>PercentageOfOpponents</value>
        /// Prawdopodobieństwo wylosowania ośmiornicy wynosi 65% a ducha 35%
        /// </summary>
        /// <param name="columns">ilość kolumn pól jednostkowych na planszy</param>
        private void RandomCharacters(int columns)
        {
            //wylosuj miejsce gracza musi to być miejsce znajdowania się kwadratu 2X2 z białych pól
            //zanjdź wszystkie dobre indexy takich kwadratów
            List<int> indexes = new List<int>();
            int rows = _boardBlocksTypes.Count/columns;
            for (int i = 0; i < _boardBlocksTypes.Count; i++)
            {
                int row = i/columns;
                int column = i - columns*row;
                if (column == columns - 1) continue;
                if (row == rows - 1) continue;
                if (_boardBlocksTypes[i] == BlockType.White
                    && _boardBlocksTypes[i + 1] == BlockType.White
                    && _boardBlocksTypes[(row + 1)*columns + column] == BlockType.White
                    && _boardBlocksTypes[(row + 1)*columns + column + 1] == BlockType.White)
                {
                    indexes.Add(i);
                }
            }
            if (indexes.Count == 0)
            {
                //od nowa wygeneruj 
                GenerateGameForSpecifiedLevel(Utils.Game == null ? 0 : Utils.Game.Level);
            }
            // wylosuj z dostępnych pól pole dla gracza
            int playerField = _random.Next(indexes.Count);
            playerField = indexes[playerField];
            _characterLocations.Add(playerField, new List<Tuple<CharacterType,int>>());
            _characterLocations[playerField].Add(new Tuple<CharacterType, int>(CharacterType.Player, -1));
            // wylosuj przeciwników
            int maxOpponentAmount = PercentageOfOpponents*_boardBlocksTypes.Count/100 - 1;
            int counter = 0;
            // każdy przeciwnik jest oddalony o minimum 5 pól od gracza w kazdym kierunku
            int destinationFromPlayer = 5;
            int playerRow = playerField/columns;
            int playerColumn = playerField - playerRow*columns;
            List<int> goodOpponentLocations = new List<int>();
            for (int index = 0; index < _boardBlocksTypes.Count; index++)
            {
                if (_boardBlocksTypes[index].Equals(BlockType.White) && !_characterLocations.ContainsKey(index))
                {
                    // sprawdź czy jest dobra odległość od gracza
                    int opponentRow = index/columns;
                    int opponentColumn = index - opponentRow*columns;
                    if (opponentRow < playerRow + destinationFromPlayer &&
                        opponentRow > playerRow - destinationFromPlayer
                        && opponentColumn < playerColumn + destinationFromPlayer &&
                        opponentColumn > playerColumn - destinationFromPlayer)
                        continue;
                    goodOpponentLocations.Add(index);
                }
            }
            if (goodOpponentLocations.Count < maxOpponentAmount + 1)
                GenerateGameForSpecifiedLevel(Utils.Game == null ? 0 : Utils.Game.Level);
            while (counter < maxOpponentAmount)
            {
                int number = _random.Next()%100;
                var characterType = number < 65 ? CharacterType.Octopus : CharacterType.Ghost;
                int opponentPosition = _random.Next(goodOpponentLocations.Count);
                opponentPosition = goodOpponentLocations[opponentPosition];
                _characterLocations.Add(opponentPosition, new List<Tuple<CharacterType,int>>()
                {
                    new Tuple<CharacterType,int>(characterType, -1)
                });
                goodOpponentLocations.Remove(opponentPosition);
                counter++;
            }
        }

        #endregion
    }
}
