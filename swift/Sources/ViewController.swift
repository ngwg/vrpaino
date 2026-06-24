import UIKit
import ARKit
import RealityKit

class ViewController: UIViewController {

    private var arView: ARView!
    private var statusLabel: UILabel!

    private var isPlaced = false
    private var indicatorAnchor: AnchorEntity?
    private var indicatorEntity: ModelEntity?

    private var pianoEntity: PianoEntity?
    private var noteSpawner: NoteSpawner?

    private var displayLink: CADisplayLink?
    private var lastTimestamp: CFTimeInterval = 0

    // MARK: - Lifecycle

    override func viewDidLoad() {
        super.viewDidLoad()
        setupARView()
        setupStatusLabel()
        setupDisplayLink()
    }

    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        let config = ARWorldTrackingConfiguration()
        config.planeDetection = [.horizontal]
        arView.session.run(config, options: [.resetTracking, .removeExistingAnchors])
    }

    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        arView.session.pause()
    }

    // MARK: - Setup

    private func setupARView() {
        arView = ARView(frame: view.bounds, cameraMode: .ar, automaticallyConfigureSession: false)
        arView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        view.addSubview(arView)
        arView.session.delegate = self

        let tap = UITapGestureRecognizer(target: self, action: #selector(handleTap(_:)))
        arView.addGestureRecognizer(tap)
    }

    private func setupStatusLabel() {
        statusLabel = UILabel()
        statusLabel.text = "  Move phone over a flat surface  "
        statusLabel.textColor = .white
        statusLabel.backgroundColor = UIColor(white: 0, alpha: 0.65)
        statusLabel.textAlignment = .center
        statusLabel.numberOfLines = 2
        statusLabel.font = .systemFont(ofSize: 16, weight: .semibold)
        statusLabel.layer.cornerRadius = 10
        statusLabel.clipsToBounds = true
        statusLabel.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(statusLabel)
        NSLayoutConstraint.activate([
            statusLabel.centerXAnchor.constraint(equalTo: view.centerXAnchor),
            statusLabel.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: 16),
            statusLabel.widthAnchor.constraint(lessThanOrEqualTo: view.widthAnchor, constant: -40),
            statusLabel.heightAnchor.constraint(greaterThanOrEqualToConstant: 48),
        ])
    }

    private func setupDisplayLink() {
        displayLink = CADisplayLink(target: self, selector: #selector(onFrame(_:)))
        displayLink?.add(to: .main, forMode: .default)
    }

    // MARK: - Per-frame update

    @objc private func onFrame(_ link: CADisplayLink) {
        let dt: Float
        if lastTimestamp == 0 {
            dt = 0
        } else {
            dt = Float(link.timestamp - lastTimestamp)
        }
        lastTimestamp = link.timestamp
        guard dt > 0, dt < 0.1 else { return }

        if !isPlaced {
            updatePlacementIndicator()
        } else {
            noteSpawner?.update(deltaTime: dt)
        }
    }

    // MARK: - Placement indicator

    private func updatePlacementIndicator() {
        let center = CGPoint(x: arView.bounds.midX, y: arView.bounds.midY)
        let hits = arView.raycast(from: center, allowing: .estimatedPlane, alignment: .horizontal)

        if let hit = hits.first {
            if indicatorAnchor == nil {
                let mesh = MeshResource.generateBox(width: 0.24, height: 0.003, depth: 0.24)
                var mat = SimpleMaterial()
                mat.color = .init(tint: UIColor.green.withAlphaComponent(0.75))
                let entity = ModelEntity(mesh: mesh, materials: [mat])
                let anchor = AnchorEntity(world: hit.worldTransform)
                anchor.addChild(entity)
                arView.scene.addAnchor(anchor)
                indicatorAnchor = anchor
                indicatorEntity = entity
            } else {
                indicatorAnchor?.transform = Transform(matrix: hit.worldTransform)
            }
            indicatorAnchor?.isEnabled = true
            statusLabel.text = "  Tap to place piano here  "
        } else {
            indicatorAnchor?.isEnabled = false
            statusLabel.text = "  Move phone over a flat surface  "
        }
    }

    // MARK: - Tap to place

    @objc private func handleTap(_ recognizer: UITapGestureRecognizer) {
        guard !isPlaced else { return }
        let point = recognizer.location(in: arView)

        // Prefer an already-detected plane surface, fall back to estimated
        let hit = arView.raycast(from: point, allowing: .existingPlaneGeometry, alignment: .horizontal).first
                ?? arView.raycast(from: point, allowing: .estimatedPlane, alignment: .horizontal).first
        guard let h = hit else { return }
        placePiano(at: h.worldTransform)
    }

    // MARK: - Place piano

    private func placePiano(at worldTransform: float4x4) {
        isPlaced = true
        indicatorAnchor?.removeFromParent()
        indicatorAnchor = nil
        statusLabel.isHidden = true

        // World-space anchor — ARKit keeps this locked to the physical surface
        let anchor = AnchorEntity(world: worldTransform)
        arView.scene.addAnchor(anchor)

        let piano = PianoEntity()

        // Rotate piano to face the camera (yaw only, no tilt)
        let camPos  = arView.cameraTransform.translation
        let pianoPt = SIMD3<Float>(worldTransform.columns.3.x,
                                   worldTransform.columns.3.y,
                                   worldTransform.columns.3.z)
        let dx = camPos.x - pianoPt.x
        let dz = camPos.z - pianoPt.z
        piano.orientation = simd_quatf(angle: atan2(dx, dz), axis: [0, 1, 0])

        anchor.addChild(piano)
        pianoEntity = piano

        noteSpawner = NoteSpawner(piano: piano)
    }
}

// MARK: - ARSessionDelegate

extension ViewController: ARSessionDelegate {
    func session(_ session: ARSession, didFailWithError error: Error) {
        DispatchQueue.main.async { [weak self] in
            self?.statusLabel.isHidden = false
            self?.statusLabel.text = "AR error: \(error.localizedDescription)"
        }
    }

    func sessionWasInterrupted(_ session: ARSession) {
        DispatchQueue.main.async { [weak self] in
            self?.statusLabel.isHidden = false
            self?.statusLabel.text = "Session interrupted"
        }
    }

    func sessionInterruptionEnded(_ session: ARSession) {
        DispatchQueue.main.async { [weak self] in
            guard let self = self, self.isPlaced else { return }
            self.statusLabel.isHidden = true
        }
    }
}
