using Kitware.VTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace ActiViz.WpfExample.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private vtkRenderWindow _renderWindow;
        private vtkRenderer _renderer;
        private vtkCubeSource _cube;
        private RelayCommand _zoomToFitCommand;
        private RelayCommand _frontCommand;
        private RelayCommand _backCommand;
        private RelayCommand _topCommand;
        private RelayCommand _bottomCommand;
        private RelayCommand _leftCommand;
        private RelayCommand _rightCommand;
        private RelayCommand _isometricCommand;
        private RelayCommand _openCommand;
        private RelayCommand _parallelepipedCommand;
        private RelayCommand _clearCommand;
        private RelayCommand _rotateCommand;

        private double _cubeLength = 1.0;
        private double _cubeWidth = 1.5;
        private double _cubeHeight = 2.0;
        private bool cubeDesignMode;

        public MainWindowViewModel()
        {
        }

        public double CubeLength
        {
            get => _cubeLength;
            set
            {
                _cubeLength = value;
                SetLength();
            }
        }
        public double CubeWidth
        {
            get => _cubeWidth;
            set
            {
                _cubeWidth = value;
                SetWidth();
            }
        }
        public double CubeHeight
        {
            get => _cubeHeight;
            set
            {
                _cubeHeight = value;
                SetHeight();
            }
        }
        public bool CubeDesignMode
        {
            get => cubeDesignMode; set
            {
                cubeDesignMode = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ZoomToFitCommand => _zoomToFitCommand ?? (_zoomToFitCommand = new RelayCommand(parametr =>
        {
            ZoomToFit();
        }));
        public RelayCommand FrontCommand => _frontCommand ?? (_frontCommand = new RelayCommand(parametr =>
        {
            SetFrontBack(true);
        }));
        public RelayCommand BackCommand => _backCommand ?? (_backCommand = new RelayCommand(parametr =>
        {
            SetFrontBack(false);
        }));
        public RelayCommand TopCommand => _topCommand ?? (_topCommand = new RelayCommand(parametr =>
        {
            SetTopBottom(true);
        }));
        public RelayCommand BottomCommand => _bottomCommand ?? (_bottomCommand = new RelayCommand(parametr =>
        {
            SetTopBottom(false);
        }));
        public RelayCommand LeftCommand => _leftCommand ?? (_leftCommand = new RelayCommand(parametr =>
        {
            SetLeftRight(true);
        }));
        public RelayCommand RightCommand => _rightCommand ?? (_rightCommand = new RelayCommand(parametr =>
        {
            SetLeftRight(false);
        }));
        public RelayCommand IsometriсCommand => _isometricCommand ?? (_isometricCommand = new RelayCommand(parametr =>
        {
            vtkCamera camera = _renderer.GetActiveCamera();
            double[] fPos = camera.GetFocalPoint();

            camera.SetPosition(fPos[0] + 1, fPos[1] + 1, fPos[2] + 1);
            double[] up = camera.GetViewUp();

            int maxID = -1;
            double max = double.MinValue;
            for (int i = 0; i < up.Length; i++)
            {
                if (Math.Abs(up[i]) > max)
                {
                    max = Math.Abs(up[i]);
                    maxID = i;
                }
            }

            if (maxID == 0)
            {
                camera.SetViewUp(Math.Sign(Math.Abs(up[0])), 0, 0);
            }
            else if (maxID == 1)
            {
                camera.SetViewUp(0, Math.Sign(Math.Abs(up[1])), 0);
            }
            else
            {
                camera.SetViewUp(0, 0, Math.Sign(Math.Abs(up[2])));
            }

            ZoomToFit();
        }));
        public RelayCommand OpenCommand => _openCommand ?? (_openCommand = new RelayCommand(parametr =>
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StlModels"),
                Filter = "STL files (*.stl)|*.stl"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (parametr is WindowsFormsHost renderControl)
                {
                    InitializeViewModel(renderControl);
                    ReadSTL(openFileDialog.FileName);
                    CubeDesignMode = false;
                }
            }
        }));
        public RelayCommand ParallelepipedCommand => _parallelepipedCommand ?? (_parallelepipedCommand = new RelayCommand(parametr =>
        {
            if (parametr is WindowsFormsHost renderControl)
            {
                InitializeViewModel(renderControl);
                DrawParallelepiped();
                CubeDesignMode = true;
            }
        }));
        public RelayCommand ClearCommand => _clearCommand ?? (_clearCommand = new RelayCommand(parametr =>
        {
            if (parametr is WindowsFormsHost renderControl)
            {
                InitializeViewModel(renderControl);
            }
            //_renderer.Clear();
            //_renderWindow.RemoveRenderer(_renderer);
        }));
        public RelayCommand RotateCommand => _rotateCommand ?? (_rotateCommand = new RelayCommand(parametr =>
        {
            Rotate();
        }));

        private void ReadSTL(string filePath)
        {
            vtkSTLReader reader = vtkSTLReader.New();
            reader.SetFileName(filePath);
            reader.Update();

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(reader.GetOutputPort());

            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            // add our actor to the renderer
            _renderer.AddActor(actor);
            ZoomToFit();
        }
        public void InitializeViewModel(RenderWindowControl renderWindowControl)
        {
            _renderWindow = renderWindowControl.RenderWindow;
            _renderer = _renderWindow.GetRenderers().GetFirstRenderer();
            _renderer.SetBackground(.2, .3, .4);

            //DrawCube();
        }
        private void InitializeViewModel(WindowsFormsHost renderControl)
        {
            Dispose();
            RenderWindowControl renderWindowControl = new RenderWindowControl();
            renderControl.Child = renderWindowControl;
            InitializeViewModel(renderWindowControl);

            //DrawCube();
        }

        private void DrawParallelepiped()
        {
            _cube = vtkCubeSource.New();
            _cube.SetXLength(CubeLength);
            _cube.SetYLength(CubeHeight);
            _cube.SetZLength(CubeWidth);

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(_cube.GetOutputPort());

            // The actor links the data pipeline to the rendering subsystem
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);

            // Create components of the rendering subsystem
            // Add the actors to the renderer
            _renderer.AddActor(actor);
            ZoomToFit();
        }
        private void SetLength()
        {
            _cube.SetXLength(CubeLength);
            _renderWindow.Render();
        }
        private void SetWidth()
        {
            _cube.SetZLength(CubeWidth);
            _renderWindow.Render();
        }
        private void SetHeight()
        {
            _cube.SetYLength(CubeHeight);
            _renderWindow.Render();
        }
        private void ZoomToFit()
        {
            _renderer.ResetCamera();
            _renderWindow.Render();
        }
        private void SetFrontBack(bool front)
        {
            int delta = front ? 1 : -1;

            vtkCamera camera = _renderer.GetActiveCamera();

            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0], fPos[1], fPos[2] + delta);
            camera.SetViewUp(0, 1, 0);
            ZoomToFit();
        }
        private void Rotate()
        {
            vtkAnimationScene animationScene = vtkAnimationScene.New();
            animationScene.SetModeToRealTime();
            animationScene.SetLoop(0);
            animationScene.SetFrameRate(5);
            animationScene.SetStartTime(0);
            animationScene.SetEndTime(20);
            //scene->AddObserver(vtkCommand::AnimationCueTickEvent, renWin.GetPointer(), &vtkWindow::Render);

            vtkAnimationCue animationCue = vtkAnimationCue.New();
            animationCue.SetStartTime(0);
            animationCue.SetEndTime(20);
            animationScene.AddCue(animationCue);

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCameraActor cameraActor = vtkCameraActor.New();
            cameraActor.SetCamera(camera);

            animationCue.StartAnimationCueEvt += AnimationCue_StartAnimationCueEvt;

            animationScene.Play();
            animationScene.Stop();
        }

        private void AnimationCue_StartAnimationCueEvt(vtkObject sender, vtkObjectEventArgs e)
        {
           
        }

        private void SetTopBottom(bool top)
        {
            int delta = top ? 1 : -1;

            vtkCamera camera = _renderer.GetActiveCamera();

            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0], fPos[1] + delta, fPos[2]);
            camera.SetViewUp(0, 0, -delta);
            ZoomToFit();
        }
        private void SetLeftRight(bool left)
        {
            int delta = left ? -1 : 1;

            vtkCamera camera = _renderer.GetActiveCamera();

            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0] + delta, fPos[1], fPos[2]);
            camera.SetViewUp(0, 1, 0);
            ZoomToFit();
        }

        public void Dispose()
        {
            _cube?.Dispose();
            _renderer?.Dispose();
            _renderWindow?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
