from cryptography.hazmat.backends import default_backend
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes

PUBLIC_EXPONENT = 65537
KEY_SIZE = 4096


def get_pin() -> int:
    DEFAULT_PIN = 12345
    pin = input(f"Default PIN: {DEFAULT_PIN} [click Enter] \nYour Pin:")

    if pin == "":
        pin = DEFAULT_PIN
    else:
        pin = int(pin)

    return pin


def generate_rsa_keys(pin: int) -> (bytes, bytes):
    private_key = rsa.generate_private_key(
        public_exponent=PUBLIC_EXPONENT,
        key_size=KEY_SIZE,
        backend=default_backend()
    )
    public_key = private_key.public_key()

    aes_key = pin.to_bytes(32, 'big')
    initialization_vector = pin.to_bytes(16, 'big')

    cipher = Cipher(algorithms.AES(aes_key), modes.CFB(initialization_vector), backend=default_backend())
    encryptor = cipher.encryptor()

    private_key_pem = private_key.private_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PrivateFormat.PKCS8,
        encryption_algorithm=serialization.NoEncryption()
    )
    encrypted_private_key = encryptor.update(private_key_pem) + encryptor.finalize()

    public_key_pem = public_key.public_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PublicFormat.SubjectPublicKeyInfo
    )

    return encrypted_private_key, public_key_pem


def save_keys(private_key: bytes, public_key: bytes) -> None:
    with open('private_key.pem', 'wb') as f:
        f.write(b"-----BEGIN RSA PRIVATE KEY-----")
        f.write(private_key)
        f.write(b"-----END RSA PRIVATE KEY-----")

    with open('public_key.pem', 'wb') as f:
        f.write(public_key)


if __name__ == "__main__":
    pin = get_pin()
    encrypted_private_key, public_key_pem = generate_rsa_keys(pin)
    save_keys(encrypted_private_key, public_key_pem)
