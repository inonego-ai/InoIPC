use inoipc::{TestTransport, frame_receive, frame_send};

#[test]
fn send_receive_round_trip() {
    let mut t = TestTransport::new();
    frame_send(&mut t, "{\"test\":\"ping\"}").unwrap();
    let result = frame_receive(&mut t).unwrap();
    assert_eq!(result, "{\"test\":\"ping\"}");
}

#[test]
fn send_receive_empty_body() {
    let mut t = TestTransport::new();
    frame_send(&mut t, "").unwrap();
    let result = frame_receive(&mut t).unwrap();
    assert_eq!(result, "");
}

#[test]
fn send_receive_unicode() {
    let mut t = TestTransport::new();
    let input = "{\"msg\":\"한글 테스트 🎉\"}";
    frame_send(&mut t, input).unwrap();
    let result = frame_receive(&mut t).unwrap();
    assert_eq!(result, input);
}

#[test]
fn send_receive_large_payload() {
    let mut t = TestTransport::new();
    let input: String = "x".repeat(100_000);
    frame_send(&mut t, &input).unwrap();
    let result = frame_receive(&mut t).unwrap();
    assert_eq!(result, input);
}

#[test]
fn send_receive_multiple_frames() {
    let mut t = TestTransport::new();
    frame_send(&mut t, "first").unwrap();
    frame_send(&mut t, "second").unwrap();
    frame_send(&mut t, "third").unwrap();
    assert_eq!(frame_receive(&mut t).unwrap(), "first");
    assert_eq!(frame_receive(&mut t).unwrap(), "second");
    assert_eq!(frame_receive(&mut t).unwrap(), "third");
}
