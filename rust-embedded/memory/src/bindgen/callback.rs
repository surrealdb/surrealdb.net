use surrealdb::sql::Value;
use surrealdb_core::rpc::format::cbor::Cbor;

use super::{alloc::alloc_u8_buffer, byte_buffer::ByteBuffer};

type GCHandlePtr = isize;

#[repr(C)]
pub struct RustGCHandle {
    ptr: GCHandlePtr,
    drop_callback: extern "C" fn(GCHandlePtr),
}

impl Drop for RustGCHandle {
    fn drop(&mut self) {
        (self.drop_callback)(self.ptr);
    }
}

#[repr(C)]
pub struct SuccessAction {
    handle: RustGCHandle,
    callback: unsafe extern "C" fn(GCHandlePtr, *mut ByteBuffer),
}

impl SuccessAction {
    pub fn invoke(&self, value: *mut ByteBuffer) {
        unsafe { (self.callback)(self.handle.ptr, value) };
    }
}

#[repr(C)]
pub struct FailureAction {
    handle: RustGCHandle,
    callback: unsafe extern "C" fn(GCHandlePtr, *mut ByteBuffer),
}

impl FailureAction {
    pub fn invoke(&self, value: *mut ByteBuffer) {
        unsafe { (self.callback)(self.handle.ptr, value) };
    }
}

fn value_to_buffer(value: Value) -> Result<*mut ByteBuffer, ()> {
    let value: Cbor = value.try_into().map_err(|_| ())?;

    let mut output = Vec::new();
    ciborium::into_writer(&value.0, &mut output).map_err(|_| ())?;

    Ok(alloc_u8_buffer(output))
}

pub fn send_success(value: Value, success: SuccessAction, failure: FailureAction) {
    match value_to_buffer(value) {
        Ok(buffer) => success.invoke(buffer),
        Err(_) => send_failure("Failed to serialize Value", failure),
    }
}

pub fn send_failure(error: &str, action: FailureAction) {
    let value = Value::Strand(error.into());

    match value_to_buffer(value) {
        Ok(buffer) => action.invoke(buffer),
        Err(_) => panic!("Failed to serialize Value"),
    }
}
