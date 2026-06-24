import RealityKit
import UIKit

struct PianoKey {
    let entity: ModelEntity
    let noteIndex: Int
    let isBlack: Bool
}

class PianoEntity: Entity {

    var whiteKeys: [PianoKey] = []
    var allKeys:   [PianoKey] = []

    // Key dimensions (metres)
    private static let wkW: Float = 0.030   // white key width
    private static let wkH: Float = 0.012   // white key height
    private static let wkD: Float = 0.140   // white key depth
    private static let bkW: Float = 0.018   // black key width
    private static let bkH: Float = 0.016   // black key height
    private static let bkD: Float = 0.090   // black key depth
    private static let octaves = 2

    // Which semitones in an octave are black keys
    private static let isBlack = [false, true, false, true, false,
                                   false, true, false, true, false, true, false]

    required init() {
        super.init()
        buildPiano()
    }

    private func buildPiano() {
        let totalWhite = 7 * Self.octaves
        let totalWidth = Float(totalWhite) * Self.wkW
        let startX     = -totalWidth / 2.0
        var whiteIdx   = 0
        var noteIdx    = 0

        for _ in 0..<Self.octaves {
            for semi in 0..<12 {
                let black = Self.isBlack[semi]
                if !black {
                    let x   = startX + (Float(whiteIdx) + 0.5) * Self.wkW
                    let key = makeKey(x: x, y: 0, z: 0,
                                      w: Self.wkW - 0.002, h: Self.wkH, d: Self.wkD,
                                      isBlack: false, index: noteIdx)
                    whiteKeys.append(key)
                    allKeys.append(key)
                    addChild(key.entity)
                    whiteIdx += 1
                } else {
                    // Black key sits between the two adjacent white keys
                    let x   = startX + (Float(whiteIdx) - 0.5) * Self.wkW
                    let y   = Self.bkH / 2.0
                    let z   = (Self.wkD - Self.bkD) / 2.0 - 0.010
                    let key = makeKey(x: x, y: y, z: z,
                                      w: Self.bkW, h: Self.bkH, d: Self.bkD,
                                      isBlack: true, index: noteIdx)
                    allKeys.append(key)
                    addChild(key.entity)
                }
                noteIdx += 1
            }
        }
    }

    private func makeKey(x: Float, y: Float, z: Float,
                         w: Float, h: Float, d: Float,
                         isBlack: Bool, index: Int) -> PianoKey {
        let mesh = MeshResource.generateBox(width: w, height: h, depth: d)
        var mat  = SimpleMaterial()
        mat.color = .init(tint: isBlack ? .black : .white)
        let entity = ModelEntity(mesh: mesh, materials: [mat])
        entity.position = SIMD3<Float>(x, y, z)
        return PianoKey(entity: entity, noteIndex: index, isBlack: isBlack)
    }

    func flashKey(at whiteIndex: Int) {
        guard whiteIndex < whiteKeys.count else { return }
        let key = whiteKeys[whiteIndex]
        var flashMat = SimpleMaterial()
        flashMat.color = .init(tint: .cyan)
        key.entity.model?.materials = [flashMat]
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.25) {
            var resetMat = SimpleMaterial()
            resetMat.color = .init(tint: key.isBlack ? .black : .white)
            key.entity.model?.materials = [resetMat]
        }
    }
}
