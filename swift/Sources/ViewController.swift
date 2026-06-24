import UIKit
import ARKit
import RealityKit
import AVFoundation

class ViewController: UIViewController {

    // MARK: - UI

    private var arView: ARView?
    private var statusLabel: UILabel!
    private var debugLabel:  UILabel!

    // MARK: - Game state

    private var isPlaced        = false
    private var didStartSession = false
    private var indicatorAnchor: AnchorEntity?
    private var pianoEntity:     PianoEntity?
    private var noteSpawner:     NoteSpawner?

    private var displayLink: CADisplayLink?
    private var lastTimestamp: CFTimeInterval = 0

    // MARK: - Lifecycle

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = .black
        setupLabels()
        setStatus("Starting…")
        setDebug("waiting for viewDidAppear")
    }

    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)

        // ARView/Metal needs the view to be fully in the window hierarchy.
        guard arView == nil else { return }
        bootstrap()
    }

    // MARK: - Bootstrap (called once viewDidAppear is safe)

    private func bootstrap() {
        setDebug("checking ARKit support")
        guard ARWorldTrackingConfiguration.isSupported else {
            setStatus("ARKit is not supported on this device.")
            return
        }

        setDebug("checking camera permission")
        switch AVCaptureDevice.authorizationStatus(for: .video) {
        case .authorized:
            buildARView()
        case .notDetermined:
            setStatus("Tap Allow when iOS asks for camera access.")
            AVCaptureDevice.requestAccess(for: .video) { [weak self] granted in
                DispatchQueue.main.async {
                    if granted {
                        self?.buildARView()
                    } else {
                        self?.setStatus("Camera denied.\nSettings › Privacy › Camera › enable for this app.")
                    }
                }
            }
        case .denied, .restricted:
            setStatus("Camera denied.\nSettings › Privacy › Camera › enable for this app.")
        @unknown default:
            buildARView()
        }
    }

    // MARK: - AR view creation

    private func buildARView() {
        setDebug("creating ARView")
        let ar = ARView(frame: view.bounds,
                        cameraMode: .ar,
                        automaticallyConfigureSession: false)
        ar.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        ar.renderOptions = [.disableMotionBlur, .disableDepthOfField]
        view.insertSubview(ar, at: 0)            // behind labels
        ar.session.delegate = self
        arView = ar

        let tap = UITapGestureRecognizer(target: self, action: #selector(handleTap(_:)))
        ar.addGestureRecognizer(tap)

        setDebug("running AR session")
        let config = ARWorldTrackingConfiguration()
        config.planeDetection = [.horizontal]
        if ARWorldTrackingConfiguration.supportsFrameSemantics(.sceneDepth) {
            config.frameSemantics.insert(.sceneDepth)
        }
        ar.session.run(config, options: [.resetTracking, .removeExistingAnchors])

        startDisplayLink()
        setStatus("Move phone over a flat surface")
    }

    // MARK: - Display link

    private func startDisplayLink() {
        displayLink?.invalidate()
        let link = CADisplayLink(target: self, selector: #selector(onFrame(_:)))
        link.add(to: .main, forMode: .default)
        displayLink = link
    }

    @objc private func onFrame(_ link: CADisplayLink) {
        guard let ar = arView else { return }

        let dt: Float
        if lastTimestamp == 0 { dt = 0 } else { dt = Float(link.timestamp - lastTimestamp) }
        lastTimestamp = link.timestamp
        guard dt > 0, dt < 0.1 else { return }

        // Wait for first AR frame before doing raycasts (avoids race-on-startup crash)
        guard ar.session.currentFrame != nil else { return }

        if !isPlaced {
            updatePlacementIndicator(ar)
        } else {
            noteSpawner?.update(deltaTime: dt)
        }
    }

    // MARK: - Placement indicator

    private func updatePlacementIndicator(_ ar: ARView) {
        let centre = CGPoint(x: ar.bounds.midX, y: ar.bounds.midY)
        let hits = ar.raycast(from: centre,
                              allowing: .estimatedPlane,
                              alignment: .horizontal)

        guard let hit = hits.first else {
            indicatorAnchor?.isEnabled = false
            setStatus("Move phone over a flat surface")
            return
        }

        if indicatorAnchor == nil {
            let mesh = MeshResource.generateBox(width: 0.30, height: 0.003, depth: 0.16)
            let mat  = SimpleMaterial(color: UIColor.systemGreen.withAlphaComponent(0.7),
                                      isMetallic: false)
            let entity = ModelEntity(mesh: mesh, materials: [mat])
            let anchor = AnchorEntity(world: hit.worldTransform)
            anchor.addChild(entity)
            ar.scene.addAnchor(anchor)
            indicatorAnchor = anchor
        } else {
            indicatorAnchor?.transform = Transform(matrix: hit.worldTransform)
        }
        indicatorAnchor?.isEnabled = true
        setStatus("Tap anywhere to place piano")
    }

    // MARK: - Tap to place

    @objc private func handleTap(_ recognizer: UITapGestureRecognizer) {
        guard !isPlaced, let ar = arView else { return }
        let point = recognizer.location(in: ar)
        let hit = ar.raycast(from: point, allowing: .existingPlaneGeometry, alignment: .horizontal).first
                ?? ar.raycast(from: point, allowing: .estimatedPlane,      alignment: .horizontal).first
        guard let h = hit else { return }
        placePiano(at: h.worldTransform, ar: ar)
    }

    // MARK: - Place piano

    private func placePiano(at worldTransform: float4x4, ar: ARView) {
        isPlaced = true
        indicatorAnchor?.removeFromParent()
        indicatorAnchor = nil

        let camPos = ar.cameraTransform.translation
        let px = worldTransform.columns.3.x
        let pz = worldTransform.columns.3.z
        let angle = atan2(camPos.x - px, camPos.z - pz)

        let anchor = AnchorEntity(world: worldTransform)
        ar.scene.addAnchor(anchor)

        let piano = PianoEntity()
        piano.orientation = simd_quatf(angle: angle, axis: [0, 1, 0])
        anchor.addChild(piano)
        pianoEntity  = piano
        noteSpawner  = NoteSpawner(piano: piano)
        setStatus("")
    }

    // MARK: - Labels

    private func setupLabels() {
        statusLabel = makeLabel(size: 16, bottom: false)
        debugLabel  = makeLabel(size: 11, bottom: true)
        debugLabel.backgroundColor = UIColor.black.withAlphaComponent(0.55)
        debugLabel.textColor = .white
    }

    private func makeLabel(size: CGFloat, bottom: Bool) -> UILabel {
        let label = UILabel()
        label.textColor = .black
        label.backgroundColor = UIColor.white.withAlphaComponent(0.90)
        label.textAlignment = .center
        label.numberOfLines = 0
        label.font = .systemFont(ofSize: size, weight: .semibold)
        label.layer.cornerRadius = 10
        label.clipsToBounds = true
        label.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(label)
        if bottom {
            NSLayoutConstraint.activate([
                label.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 8),
                label.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -8),
                label.bottomAnchor.constraint(equalTo: view.safeAreaLayoutGuide.bottomAnchor, constant: -8),
                label.heightAnchor.constraint(greaterThanOrEqualToConstant: 32),
            ])
        } else {
            NSLayoutConstraint.activate([
                label.centerXAnchor.constraint(equalTo: view.centerXAnchor),
                label.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: 16),
                label.widthAnchor.constraint(equalTo: view.widthAnchor, multiplier: 0.85),
                label.heightAnchor.constraint(greaterThanOrEqualToConstant: 50),
            ])
        }
        return label
    }

    private func setStatus(_ text: String) {
        DispatchQueue.main.async { [weak self] in
            self?.statusLabel?.text = text.isEmpty ? "" : "  \(text)  "
            self?.statusLabel?.isHidden = text.isEmpty
        }
    }

    private func setDebug(_ text: String) {
        DispatchQueue.main.async { [weak self] in
            self?.debugLabel?.text = "  \(text)  "
        }
    }
}

// MARK: - ARSessionDelegate

extension ViewController: ARSessionDelegate {

    func session(_ session: ARSession, didUpdate frame: ARFrame) {
        if !didStartSession {
            didStartSession = true
            setDebug("AR rendering ✓ tracking=\(trackingState(frame.camera.trackingState))")
        }
    }

    func session(_ session: ARSession, didFailWithError error: Error) {
        setStatus("AR error")
        setDebug("error: \(error.localizedDescription)")
    }

    func sessionWasInterrupted(_ session: ARSession) {
        setDebug("session interrupted")
    }

    func sessionInterruptionEnded(_ session: ARSession) {
        setDebug("session resumed")
    }

    func session(_ session: ARSession, cameraDidChangeTrackingState camera: ARCamera) {
        setDebug("tracking=\(trackingState(camera.trackingState))")
    }

    private func trackingState(_ s: ARCamera.TrackingState) -> String {
        switch s {
        case .normal: return "normal"
        case .notAvailable: return "not-available"
        case .limited(let r):
            switch r {
            case .excessiveMotion: return "limited:motion"
            case .insufficientFeatures: return "limited:features"
            case .initializing: return "limited:initializing"
            case .relocalizing: return "limited:relocalizing"
            @unknown default: return "limited:?"
            }
        }
    }
}
