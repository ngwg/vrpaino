import RealityKit
import UIKit

class NoteSpawner {

    private weak var piano: PianoEntity?
    private var activeNotes: [(entity: ModelEntity, whiteKeyIndex: Int)] = []

    private var elapsed:      Float = 0
    private var nextNoteTime: Float = 1.0  // 1 s startup pause
    private var songIndex:    Int   = 0

    private let bpm:        Float = 75
    private let fallHeight: Float = 0.70
    private let fallSpeed:  Float = 1.00

    // Twinkle Twinkle Little Star — white-key indices (0 = C4)
    private static let song: [Int] = [
        0,0,4,4,5,5,4,  3,3,2,2,1,1,0,
        4,4,3,3,2,2,1,  4,4,3,3,2,2,1,
        0,0,4,4,5,5,4,  3,3,2,2,1,1,0,
    ]
    private static let beats: [Float] = [
        1,1,1,1,1,1,2,  1,1,1,1,1,1,2,
        1,1,1,1,1,1,2,  1,1,1,1,1,1,2,
        1,1,1,1,1,1,2,  1,1,1,1,1,1,2,
    ]
    private static let noteColors: [UIColor] = [
        UIColor(red: 1.0, green: 0.2, blue: 0.2, alpha: 1),
        UIColor(red: 1.0, green: 0.6, blue: 0.1, alpha: 1),
        UIColor(red: 1.0, green: 1.0, blue: 0.1, alpha: 1),
        UIColor(red: 0.2, green: 1.0, blue: 0.2, alpha: 1),
        UIColor(red: 0.1, green: 0.7, blue: 1.0, alpha: 1),
        UIColor(red: 0.5, green: 0.1, blue: 1.0, alpha: 1),
        UIColor(red: 1.0, green: 0.2, blue: 0.8, alpha: 1),
    ]

    init(piano: PianoEntity) {
        self.piano = piano
    }

    // Called every frame from ViewController
    func update(deltaTime dt: Float) {
        elapsed += dt

        // Spawn the next note when its beat time arrives
        if elapsed >= nextNoteTime {
            spawnNote(songIndex)
            let beatDuration = 60.0 / bpm
            nextNoteTime = elapsed + beatDuration * Self.beats[songIndex]
            songIndex = (songIndex + 1) % Self.song.count
        }

        moveNotes(dt: dt)
    }

    // MARK: - Private

    private func spawnNote(_ index: Int) {
        guard let piano = piano else { return }
        let keyIdx = Self.song[index]
        guard keyIdx < piano.whiteKeys.count else { return }

        let key  = piano.whiteKeys[keyIdx]
        let mesh = MeshResource.generateBox(width: 0.025, height: 0.050, depth: 0.018)
        var mat  = SimpleMaterial()
        mat.color = .init(tint: Self.noteColors[keyIdx % Self.noteColors.count])
        let note = ModelEntity(mesh: mesh, materials: [mat])

        // Spawn above the key in piano-local space
        let kp = key.entity.position   // local to PianoEntity
        note.position = SIMD3<Float>(kp.x, kp.y + fallHeight, kp.z)

        piano.addChild(note)
        activeNotes.append((entity: note, whiteKeyIndex: keyIdx))
    }

    private func moveNotes(dt: Float) {
        guard let piano = piano else { return }
        var dead: [Int] = []

        for (i, note) in activeNotes.enumerated() {
            // Move note down in piano-local space
            note.entity.position.y -= fallSpeed * dt

            guard note.whiteKeyIndex < piano.whiteKeys.count else {
                dead.append(i); continue
            }
            let key = piano.whiteKeys[note.whiteKeyIndex]

            // Compare world-space Y so rotation doesn't confuse us
            let noteY = note.entity.position(relativeTo: nil).y
            let keyY  = key.entity.position(relativeTo: nil).y

            if noteY <= keyY + 0.02 {
                piano.flashKey(at: note.whiteKeyIndex)
                note.entity.removeFromParent()
                dead.append(i)
            } else if noteY < keyY - 0.25 {
                note.entity.removeFromParent()
                dead.append(i)
            }
        }

        for i in dead.sorted().reversed() {
            activeNotes.remove(at: i)
        }
    }
}
