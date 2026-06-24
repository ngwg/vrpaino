import UIKit
import ARKit
import RealityKit

class ViewController: UIViewController {

    // MARK: - Properties

    private var arView: ARView!
    private var statusLabel: UILabel!

    private var isPlaced        = false
    private var indicatorAnchor: AnchorEntity?
    private var indicatorEntity: ModelEntity?

    private var pianoAnchor: AnchorEntity?
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
        startARSession()
    }

    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        arView.session.pause()
        displayLink?.invalidate()
    }

    // MARK: - AR Setup

    private func setupARView() {
        arView = ARView(frame: view.bounds,
                        cameraMode: .ar,
                        automaticallyConfigureSession: false)
        arView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        view.addSubview(arView)
        arView.session.delegate = self

        let tap = UITapGestureRecognizer(target: self, action: #selector(handleTap(_:)))
        arView.addGestureRecognizer(tap)
    }

    private func startARSession() {
        let config = ARWorldTrackingConfiguration()
        config.planeDetection = [.horizontal]
        arView.session.run(config, options: [.resetTracking, .removeExistingAnchors])
        showStatus("Move phone over a flat surface")
    }

    // MARK: - Status label

    private func setupStatusLabel() {
        let label = UILabel()
        label.textColor = .black
        label.backgroundColor = UIColor.white.withAlphaComponent(0.90)
        label.textAlignment = .center
        label.numberOfLines = 0
        label.font = .systemFont(ofSize: 16, weight: .semibold)
        label.layer.cornerRadius = 12
        label.clipsToBounds = true
        label.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(label)
        NSLayoutConstraint.activate([
            label.centerXAnchor.constraint(equalTo: view.centerXAnchor),
            label.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: 16),
            label.widthAnchor.constraint(equalTo: view.widthAnchor, multiplier: 0.85),
            label.heightAnchor.constraint(greaterThanOrEqualToConstant: 50),
        ])
        statusLabel = label
    }

    private func showStatus(_ text: String) {
        DispatchQueue.main.async { [weak self] in
            self?.statusLabel?.text = "  \(text)  "
            self?.statusLabel?.isHidden = false
        }
    }

    private func hideStatus() {
        DispatchQueue.main.async { [weak self] in
            self?.statusLabel?.isHidden = true
        }
    }

    // MARK: - Display link

    private func setupDisplayLink() {
        let link = CADisplayLink(target: self, selector: #selector(onFrame(_:)))
        link.add(to: .main, forMode: .default)
        displayLink = link
    }

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
        let centre = CGPoint(x: arView.bounds.midX, y: arView.bounds.midY)
        let hits = arView.raycast(from: centre,
                                  allowing: .estimatedPlane,
                                  alignment: .horizontal)

        if let hit = hits.first {
            if indicatorAnchor == nil {
                let mesh = MeshResource.generateBox(width: 0.30, height: 0.003, depth: 0.16)
                var mat = SimpleMaterial()
                mat.color = .init(tint: UIColor.systemGreen.withAlphaComponent(0.75))
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
            showStatus("Tap to place piano here")
        } else {
            indicatorAnchor?.isEnabled = false
            showStatus("Move phone over a flat surface")
        }
    }

    // MARK: - Tap to place

    @objc private func handleTap(_ recognizer: UITapGestureRecognizer) {
        guard !isPlaced else { return }

        let point = recognizer.location(in: arView)
        let hit = arView.raycast(from: point,
                                 allowing: .existingPlaneGeometry,
                                 alignment: .horizontal).first
                ?? arView.raycast(from: point,
                                  allowing: .estimatedPlane,
                                  alignment: .horizontal).first
        guard let h = hit else { return }

        placePiano(at: h.worldTransform)
    }

    // MARK: - Place piano

    private func placePiano(at worldTransform: float4x4) {
        isPlaced = true

        // Remove indicator
        indicatorAnchor?.removeFromParent()
        indicatorAnchor = nil
        indicatorEntity = nil

        // Rotate so keyboard faces camera
        let camPos = arView.cameraTransform.translation
        let px = worldTransform.columns.3.x
        let pz = worldTransform.columns.3.z
        let angle = atan2(camPos.x - px, camPos.z - pz)

        let anchor = AnchorEntity(world: worldTransform)
        arView.scene.addAnchor(anchor)
        pianoAnchor = anchor

        let piano = PianoEntity()
        piano.orientation = simd_quatf(angle: angle, axis: [0, 1, 0])
        anchor.addChild(piano)
        pianoEntity = piano

        noteSpawner = NoteSpawner(piano: piano)
        hideStatus()
    }
}

// MARK: - ARSessionDelegate

extension ViewController: ARSessionDelegate {

    func session(_ session: ARSession, didFailWithError error: Error) {
        showStatus("AR error: \(error.localizedDescription)")
    }

    func sessionWasInterrupted(_ session: ARSession) {
        showStatus("Session interrupted")
    }

    func sessionInterruptionEnded(_ session: ARSession) {
        if isPlaced { hideStatus() }
    }
}
