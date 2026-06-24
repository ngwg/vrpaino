import UIKit
import AVFoundation

class ViewController: UIViewController {

    private var previewLayer: AVCaptureVideoPreviewLayer?
    private var label: UILabel!

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = .systemBlue

        label = UILabel()
        label.text = "Boot…"
        label.textColor = .white
        label.backgroundColor = .black
        label.font = .systemFont(ofSize: 16, weight: .bold)
        label.textAlignment = .center
        label.numberOfLines = 0
        label.frame = CGRect(x: 10, y: 60, width: view.bounds.width - 20, height: 120)
        label.autoresizingMask = [.flexibleWidth]
        view.addSubview(label)

        let rawStatus = AVCaptureDevice.authorizationStatus(for: .video).rawValue
        setLabel("App launched.\nCamera status raw = \(rawStatus)\n0=notDetermined 1=restricted 2=denied 3=authorized")
    }

    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)

        AVCaptureDevice.requestAccess(for: .video) { [weak self] granted in
            DispatchQueue.main.async {
                if granted {
                    self?.setLabel("✓ Camera GRANTED — starting preview…")
                    self?.startPreview()
                } else {
                    self?.setLabel("✗ Camera DENIED.\nIf no dialog ever appeared, the Info.plist is broken.\nElse: Settings › Privacy › Camera › enable for this app.")
                }
            }
        }
    }

    override func viewDidLayoutSubviews() {
        super.viewDidLayoutSubviews()
        previewLayer?.frame = view.bounds
    }

    private func setLabel(_ s: String) {
        label.text = "  \(s)  "
    }

    private func startPreview() {
        let session = AVCaptureSession()
        session.sessionPreset = .high

        guard
            let device = AVCaptureDevice.default(.builtInWideAngleCamera, for: .video, position: .back),
            let input  = try? AVCaptureDeviceInput(device: device),
            session.canAddInput(input)
        else {
            setLabel("Cannot open camera device.")
            return
        }
        session.addInput(input)

        let layer = AVCaptureVideoPreviewLayer(session: session)
        layer.videoGravity = .resizeAspectFill
        layer.frame = view.bounds
        view.layer.insertSublayer(layer, at: 0)
        previewLayer = layer

        DispatchQueue.global(qos: .userInitiated).async { session.startRunning() }
        setLabel("Camera running.\nIf you see this label over a live camera feed, the Info.plist is good and we can re-enable ARKit.")
    }
}
