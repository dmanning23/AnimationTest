using AnimationLib;
using AnimationsLoader;
using CollisionBuddy;
using DrawListBuddy;
using FontBuddyLib;
using GameTimer;
using HadoukInput;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PrimitiveBuddy;
using RenderBuddy;
using ResolutionBuddy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimationTest
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		#region Properties

		#region test harness stuff

		GraphicsDeviceManager graphics;
		KeyboardState lastKeyboardState;
		FontBuddy font = new FontBuddy();

		int _currentAnimation;
		float _rotation;
		EPlayback _playbackMode;
		Vector2 _position;
		private bool _renderJointSkeleton;
		private bool _renderPhysics;
		private bool _flip;

		#endregion //test harness stuff

		#region animation stuff

		AnimationLoader AnimationLoader { get; set; }

		Renderer Renderer { get; set; }
		AnimationContainer Animations => AnimationLoader.Animations;
		DrawList Drawlist { get; set; }
		GameClock Clock;
		const float lightRadius = 64f;
		const float animationRadius = 256f;

		List<string> AnimationNames { get; set; }

		#endregion //animation stuff

		#region lighting stuff

		Circle _circle1;
		Circle _circle2;
		PointLight _light;
		Primitive primitive;
		Rectangle desired = new Rectangle(0, 0, 1280, 720);
		InputState _inputState;
		ControllerWrapper _controller;
		InputWrapper _inputWrapper;

		Vector3 LightPosition { get { return new Vector3(_circle1.Pos.X, _circle1.Pos.Y, 25f); } }

		//float minBrightness = 120.1f;
		//float maxBrightness = 120.2f;

		float minBrightness = 110.201f;
		float maxBrightness = 110.202f;

		float fireDelta = 0.05f;

		float movespeed = 1200.0f;

		//float sustainTimeDelta = 7f;
		float sustainTimeDelta = -1;

		#endregion //lighting stuff

		#endregion //Properties

		#region Methods

		public Game1()
		{
			Content.RootDirectory = "Content";
			_currentAnimation = 0;
			_rotation = 0.0f;
			_playbackMode = EPlayback.Forwards;
			_position = new Vector2(0.0f);
			_renderJointSkeleton = false;
			_renderPhysics = false;
			_flip = false;
			Drawlist = new DrawList();
			Clock = new GameClock();

			graphics = new GraphicsDeviceManager(this)
			{
				GraphicsProfile = GraphicsProfile.HiDef
			};

			_inputState = new InputState();
			Mappings.UseKeyboard[0] = true;
			Mappings.UseIpacMappings(0);

			_controller = new ControllerWrapper(0);
			_inputWrapper = new InputWrapper(_controller, Clock.GetCurrentTime);

			AnimationNames = new List<string>();
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			lastKeyboardState = Keyboard.GetState();

			var resolution = new ResolutionComponent(this, graphics, new Point(1280, 720), new Point(1280, 720), false, true, false);

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			//use this.Content to load your game content here
			font.LoadContent(Content, @"ArialBlack24");

			_circle1 = new Circle();
			_circle1.Initialize(new Vector2(graphics.GraphicsDevice.Viewport.TitleSafeArea.Center.X - 700,
											graphics.GraphicsDevice.Viewport.TitleSafeArea.Center.Y), 60.0f);
			_circle2 = new Circle();
			_circle2.Initialize(new Vector2(graphics.GraphicsDevice.Viewport.TitleSafeArea.Center.X + 800,
											graphics.GraphicsDevice.Viewport.TitleSafeArea.Center.Y), 60.0f);

			Renderer = new Renderer(this, Content);
			Renderer.AmbientColor = new Color(.2f, .2f, .2f);
			Renderer.ClearLights();
			Renderer.AddDirectionalLight(new Vector3(0f, 1f, .1f), new Color(.2f, 0f, .3f));
			Renderer.AddDirectionalLight(new Vector3(-.5f, -1f, -.1f), new Color(1f, .7f, 0f, 0.75f));
			Renderer.AddDirectionalLight(new Vector3(.5f, -1f, .6f), new Color(1f, 1f, .75f));

			_light = new FirePointLight(LightPosition, Color.Red, fireDelta, minBrightness, maxBrightness);
			//Renderer.AddPointLight(_light);
			Renderer.Camera.IgnoreWorldBoundary = true;

			_position = Resolution.TitleSafeArea.Center.ToVector2();

			Renderer.LoadContent(GraphicsDevice);
			//Renderer.TextureLoader = new TextureFileLoader();

			AnimationLoader = new AnimationLoader(Renderer, 1f);
			AnimationLoader.LoadContent();

			try
			{
				//AnimationLoader.LoadMai(Content);
				AnimationLoader.LoadMaiJson(Content);
				//AnimationLoader.LoadGrimoireGoblin();
			}
			catch (Exception ex)
			{
				throw new Exception("Darn! Couldn't load the thing.", ex);
			}

			AnimationNames = Animations.Animations.Keys.ToList();

			//start the time
			Clock.Start();
			Animations.SetAnimation(AnimationNames[_currentAnimation], EPlayback.Forwards);
			primitive = new Primitive(graphics.GraphicsDevice, Renderer.SpriteBatch);

			AddPointToCamera(_circle1.Pos, lightRadius);
			AddPointToCamera(_circle2.Pos, lightRadius);
			AddPointToCamera(_position, animationRadius);
			Renderer.Camera.BeginScene(true);
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			if ((GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) ||
			Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
#if !NETFX_CORE && !__IOS__
				this.Exit();
#endif
			}

			base.Update(gameTime);

			Clock.Update(gameTime);

			//update the input
			_inputState.Update();
			_inputWrapper.Update(_inputState, false);

			Renderer.Camera.Update(Clock);
			Renderer.Update(gameTime);

			KeyboardState currentState = Keyboard.GetState();

			//move the circle


			//check veritcal movement
			if (_inputWrapper.Controller.CheckKeystrokeHeld(EKeystroke.Up))
			{
				_circle1.Translate(0.0f, -movespeed * Clock.TimeDelta);
			}
			else if (_inputWrapper.Controller.CheckKeystrokeHeld(EKeystroke.Down))
			{
				_circle1.Translate(0.0f, movespeed * Clock.TimeDelta);
			}

			//check horizontal movement
			if (_inputWrapper.Controller.CheckKeystrokeHeld(EKeystroke.Forward))
			{
				_circle1.Translate(movespeed * Clock.TimeDelta, 0.0f);
			}
			else if (_inputWrapper.Controller.CheckKeystrokeHeld(EKeystroke.Back))
			{
				_circle1.Translate(-movespeed * Clock.TimeDelta, 0.0f);
			}

			//change the play back mode?
			if (currentState.IsKeyDown(Keys.I))
			{
				_circle2.Translate(0.0f, -movespeed * Clock.TimeDelta);
			}
			else if (currentState.IsKeyDown(Keys.K))
			{
				_circle2.Translate(0.0f, movespeed * Clock.TimeDelta);
			}
			else if (currentState.IsKeyDown(Keys.L))
			{
				_circle2.Translate(movespeed * Clock.TimeDelta, 0.0f);
			}
			else if (currentState.IsKeyDown(Keys.J))
			{
				_circle2.Translate(-movespeed * Clock.TimeDelta, 0.0f);
			}


			if (currentState.IsKeyDown(Keys.A) && lastKeyboardState.IsKeyUp(Keys.A))
			{
				//increment the animation
				if (_currentAnimation < (Animations.Animations.Count - 1))
				{
					_currentAnimation++;
					Animations.SetAnimation(AnimationNames[_currentAnimation], _playbackMode);
				}
			}
			else if (currentState.IsKeyDown(Keys.Z) && lastKeyboardState.IsKeyUp(Keys.Z))
			{
				//decrement the animation
				if (_currentAnimation > 0)
				{
					_currentAnimation--;
					Animations.SetAnimation(AnimationNames[_currentAnimation], _playbackMode);
				}
			}

			////move the thing up?
			//if (currentState.IsKeyDown(Keys.Up))
			//{
			//	m_Position.Y -= 500.0f * Clock.TimeDelta;
			//}

			////move the thing down?
			//if (currentState.IsKeyDown(Keys.Down))
			//{
			//	m_Position.Y += 500.0f * Clock.TimeDelta;
			//}

			////move the thing left?
			//if (currentState.IsKeyDown(Keys.Left))
			//{
			//	m_Position.X -= 500.0f * Clock.TimeDelta;
			//}

			////move the thing right?
			//if (currentState.IsKeyDown(Keys.Right))
			//{
			//	m_Position.X += 500.0f * Clock.TimeDelta;
			//}

			//add some camera shake?
			if (currentState.IsKeyDown(Keys.Space) && lastKeyboardState.IsKeyDown(Keys.Space))
			{
				Renderer.Camera.AddCameraShake(0.25f);
			}

			//change the play back mode?
			if (currentState.IsKeyDown(Keys.Q) && lastKeyboardState.IsKeyUp(Keys.Q))
			{
				_playbackMode = EPlayback.Forwards;
				Animations.SetAnimation(AnimationNames[_currentAnimation], _playbackMode);
			}
			else if (currentState.IsKeyDown(Keys.W) && lastKeyboardState.IsKeyUp(Keys.W))
			{
				_playbackMode = EPlayback.Backwards;
				Animations.SetAnimation(AnimationNames[_currentAnimation], _playbackMode);
			}
			else if (currentState.IsKeyDown(Keys.E) && lastKeyboardState.IsKeyUp(Keys.E))
			{
				_playbackMode = EPlayback.Loop;
				Animations.SetAnimation(AnimationNames[_currentAnimation], _playbackMode);
			}
			else if (currentState.IsKeyDown(Keys.R) && lastKeyboardState.IsKeyUp(Keys.R))
			{
				_playbackMode = EPlayback.LoopBackwards;
				Animations.SetAnimation(AnimationNames[_currentAnimation], _playbackMode);
			}
			else if (currentState.IsKeyDown(Keys.T) && lastKeyboardState.IsKeyUp(Keys.T))
			{
				_playbackMode = EPlayback.LoopRandom;
				Animations.SetAnimation(AnimationNames[_currentAnimation], _playbackMode);
			}

			//rotate the thing?
			if (currentState.IsKeyDown(Keys.X))
			{
				_rotation += 4.0f * Clock.TimeDelta;
			}
			else if (currentState.IsKeyDown(Keys.C))
			{
				_rotation -= 4.0f * Clock.TimeDelta;
			}

			//draw the joints?
			if (currentState.IsKeyDown(Keys.P) && lastKeyboardState.IsKeyUp(Keys.P))
			{
				_renderJointSkeleton = !_renderJointSkeleton;
			}
			if (currentState.IsKeyDown(Keys.O) && lastKeyboardState.IsKeyUp(Keys.O))
			{
				_renderPhysics = !_renderPhysics;
			}

			//flip the model?
			if (currentState.IsKeyDown(Keys.F) && lastKeyboardState.IsKeyUp(Keys.F))
			{
				_flip = !_flip;
			}

			//Add a flare light
			if (currentState.IsKeyDown(Keys.G) && lastKeyboardState.IsKeyUp(Keys.G))
			{
				var light = new FlarePointLight(LightPosition, Color.Yellow, fireDelta, 0.25f, sustainTimeDelta, 0.25f, minBrightness, maxBrightness);
				Renderer.AddPointLight(light);
				//Renderer.AddPointLight(new FlashPointLight(LightPosition, 10f, new Color(32, 32, 255), 1f));
			}

			lastKeyboardState = currentState;

			Animations.Update(Clock, _position, _flip, _rotation, false);
			Animations.UpdateRagdoll();

			_light.Position = LightPosition;
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			graphics.GraphicsDevice.Clear(Color.DarkGray);

			//setup the camera for drawing
			AddPointToCamera(_circle1.Pos, lightRadius);
			AddPointToCamera(_circle2.Pos, lightRadius);
			AddPointToCamera(_position, animationRadius);
			Renderer.Camera.BeginScene(false);

			//set up the drawlist
			Drawlist.Set(0.0f, Color.White, 1f);
			Drawlist.Flush();

			//draw the character
			Animations.Render(Drawlist);
			Renderer.SpriteBatchBegin(BlendState.NonPremultiplied, Renderer.Camera.TranslationMatrix * Resolution.TransformationMatrix(), SpriteSortMode.Immediate);

			Drawlist.Render(Renderer);
			Renderer.SpriteBatchEnd();

			Renderer.SpriteBatchBeginNoEffect(BlendState.AlphaBlend, Resolution.TransformationMatrix());

			//draw the joint skeleton?
			if (_renderJointSkeleton)
			{
				Animations.Skeleton.RootBone.DrawSkeleton(Renderer, true, Color.White);
			}

			//draw the physics
			if (_renderPhysics)
			{
				Animations.Skeleton.RootBone.DrawPhysics(Renderer, true, Color.Red);
			}

			//draw the players circle in green
			primitive.Thickness = 3f;
			primitive.Circle(_circle1.Pos, _circle1.Radius, Color.Red);

			//write the name of the current animation
			var pos = new Vector2(Resolution.TitleSafeArea.Left, Resolution.TitleSafeArea.Top);
			if (null != Animations.CurrentAnimation)
			{
				font.Write(Animations.CurrentAnimation.Name,
						   pos, Justify.Left, 1.0f, Color.White, Renderer.SpriteBatch, Clock);
			}
			pos.Y += 24;

			Renderer.SpriteBatchEnd();

			base.Draw(gameTime);
		}

		private void AddPointToCamera(Vector2 position, float radius)
		{
			//Add the upperleft and lowercorners.  That will fit the whole circle in camera
			Renderer.Camera.AddPoint(position);
			Renderer.Camera.AddPoint(new Vector2((position.X - radius), (position.Y - radius)));
			Renderer.Camera.AddPoint(new Vector2((position.X + radius), (position.Y + radius)));
		}

		#endregion //Methods
	}
}