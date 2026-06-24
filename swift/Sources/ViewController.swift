import UIKit
import ARKit
import RealityKit

class ViewController: UIViewController {

    private var arView: ARView!
    private var label: UILabel!

    override func viewDidLoad() {
        super.viewDidLoad()

        // Bright background so we instantly see if UIKit even rendered
        view.backgroundColor = .systemBlue

        label = UILabel()
        label.text = "1. App launched"
        label.textColor = .white
        label.backgroundColor = .black
        label.font = .systemFont(ofSize: 18, weight: .bold)
        label.textAlignment = .center
        label.numberOfLines = 0
        label.frame = CGRect(x: 10, y: 60, width: view.bounds.width - 20, height: 80)
        label.autoresizingMask = [.flexibleWidth]
        view.addSubview(label)
    }

    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)

        label.text = "2. viewDidAppear"

        guard ARWorldTrackingConfiguration.isSupported else {
            label.text = "ARKit NOT supported"
            return
        }

        label.text = "3. Creating ARView…"
        arView = ARView(frame: view.bounds)
        arView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        view.insertSubview(arView, belowSubview: label)

        label.text = "4. Running AR session…"
        let config = ARWorldTrackingConfiguration()
        arView.session.run(config)

        label.text = "5. AR running — camera should show"
    }
}
