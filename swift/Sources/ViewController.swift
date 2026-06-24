import UIKit
import ARKit
import RealityKit
import AVFoundation

class ViewController: UIViewController {

    private var arView: ARView?
    private var statusLabel: UILabel!

    private var isPlaced = false
    private var indicatorAnchor: AnchorEntity?

    private var pianoEntity: PianoEntity?
    private var noteSpawner: NoteSpawner?

    private var displayLink: CADisplayLink?
    private var lastTimestamp: CFTimeInterval = 0

    // MARK: - Lifecycle

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = .black
        setupStatusLabel()
        showStatus("Requesting camera access...")
        checkCameraPermission()
    }

    // MARK: - Camera permission

    private func checkCameraPermission() {
        switch AVCaptureDevice.authorizationStatus(for: .video) {
        case .authorized:
            setupAR()
        case .notDetermined:
            AVCaptureDevice.requestAccess(for: .video) { [weak self] granted in
                DispatchQueue.main.async {
                    if granted {
                        self?.setupAR()
                    } else {
                        self?.showStatus("Camera access denied.\n\nGo to Settings › Privacy › Camera\nand enable access for this app.")
                    }
                }
            }
        case .denied, .restricted:
            showStatus("Camera access denied.\n\nGo to Settings › Privacy › Camera\nand enable access for this app.")
        @unknown default:
            setupAR()
        }
    }

    // MARK: - AR setup (called after permission granted)

    private func setupAR() {
        let ar = ARView(frame: view.bounds, cameraMode: .ar, automaticallyConfigureSession: false)
        ar.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        view.insertSubview(ar, at: 0)          // behind status label
        ar.session.delegate = self
        arView = ar

        let tap = UITapGestureRecognizer(target: self, action: #selector(handleTap(_:)))
        ar.addGestureRecognizer(tap)

        let config = ARWorldTrackingConfiguration()
        config.planeDetection = [.horizontal]
        ar.session.run(config, options: [.resetTracking, .removeExistingAnchors])

        setupDisplayLink()
        showStatus("Move phone over a flat surface")
    }

    // MARK: - Status label

    private func setupStatusLabel() {
        statusLabel = UILabel()
        statusLabel.text = "Starting..."
        statusLabel.textColor = .black
        statusLabel.backgroundColor = UIColor.white.withAlphaComponent(0.88)
        statusLabel.textAlignment = .center
        statusLabel.numberOfLines = 0
        statusLabel.font = .systemFont(ofSize: 17, weight: .semibold)
        statusLabel.layer.cornerRadius = 12
        statusLabel.clipsToBounds = true
        statusLabel.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(statusLabel)
        NSLayoutConstraint.activate([
            statusLabel.centerXAnchor.constraint(equalTo: view.centerXAnchor),
            statusLabel.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: 20),
            statusLabel.widthAnchor.constraint(equalTo: view.widthAnchor, multiplier: 0.85),
            statusLabel.heightAnchor.constraint(greaterThanOrEqualToConstant: 54),
        ])
        // Padding via insets
        statusLabel.layoutMargins = UIEdgeInsets(top: 8, left: 12, bottom: 8, right: 12)
    }

    private func showStatus(_ text: String) {
        DispatchQueue.main.async { [weak self] in
            self?.statusLabel.text = text
            self?.statusLabel.isHidden = false
        }
    }

    private func hideStatus() {
        DispatchQueue.main.async { [weak self] in
            self?.statusLabel.isHidden = true
        }
    }

    // MARK: - Display link

    private func setupDisplayLink() {
        displayLink?.invalidate()
        displayLink = CADisplayLink(target: self, selector: #selector(onFrame(_:)))
        displayLink?.add(to: .main, forMode: .default)
    }

    @objc private func onFrame(_ link: CADisplayLink) {
        guard let ar = arView else { return }
        let dt: Float
        if lastTimestamp == 0 {
            dt = 0
        } else {
            dt = Float(link.timestamp - lastTimestamp)
        }
        lastTimestamp = link.timestamp
        guard dt > 0, dt < 0.1 else { return }

        if !isPlaced {
            updatePlacementIndicator(ar: ar)
        } else {
            noteSpawner?.update(deltaTime: dt)
        }
    }

    // MARK: - Placement indicator

    private func updatePlacementIndicator(ar: ARView) {
        let center = CGPoint(x: ar.bounds.midX, y: ar.bounds.midY)
        let hits = ar.raycast(from: center, allowing: .estimatedPlane, alignment: .horizontal)

        if let hit = hits.first {
            if indicatorAnchor == nil {
                let mesh = MeshResource.generateBox(width: 0.24, height: 0.003, depth: 0.24)
                var mat = SimpleMaterial()
                mat.color = .init(tint: UIColor.green.withAlphaComponent(0.8))
                let entity = ModelEntity(mesh: mesh, materials: [mat])
                let anchor = AnchorEntity(world: hit.worldTransform)
                anchor.addChild(entity)
                ar.scene.addAnchor(anchor)
                indicatorAnchor = anchor
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
        guard !isPlaced, let ar = arView else { return }
        let point = recognizer.location(in: ar)
        let hit = ar.raycast(from: point, allowing: .existingPlaneGeometry, alignment: .horizontal).first
                ?? ar.raycast(from: point, allowing: .estimatedPlane, alignment: .horizontal).first
        guard let h = hit else { return }
        placePiano(ar: ar, worldTransform: h.worldTransform)
    }

    // MARK: - Place piano

    private func placePiano(ar: ARView, worldTransform: float4x4) {
        isPlaced = true
        indicatorAnchor?.removeFromParent()
        indicatorAnchor = nil
        hideStatus()

        let anchor = AnchorEntity(world: worldTransform)
        ar.scene.addAnchor(anchor)

        let piano = PianoEntity()
        let camPos = ar.cameraTransform.translation
        let px = worldTransform.columns.3.x
        let pz = worldTransform.columns.3.z
        piano.orientation = simd_quatf(angle: atan2(camPos.x - px, camPos.z - pz), axis: [0, 1, 0])

        anchor.addChild(piano)
        pianoEntity = piano
        noteSpawner = NoteSpawner(piano: piano)
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
