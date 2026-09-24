
document.addEventListener('DOMContentLoaded', function () {
    AOS.init();

    const navbar = document.querySelector('.navbar');
    const logo = document.querySelector('.navbar-brand img');
    const headerBackground = document.querySelector('.header-background');
    const headerTitle = document.querySelector('.header-title');
    const buyNowButton = document.querySelector('.buy-now');

    const images = [
        "images/picstv-familly/image10.jpg",
        "images/picstv-familly/image11.jpg",
        "images/picstv-familly/image12.jpg",        
        "images/picstv-familly/image3.jpg",        
    ];

    let imageIndex = 0;

    function updateBackgroundImage() {
        headerBackground.style.backgroundImage = `url(${images[imageIndex]})`;
        headerBackground.style.backgroundSize = 'cover';
        headerBackground.classList.remove('animated');
        setTimeout(() => {
            headerBackground.classList.add('animated');
        }, 100);

        imageIndex = (imageIndex + 1) % images.length;
    }

    function handleScroll() {
        const scrollPosition = window.scrollY;
        const scrollPositionb = window.scrollX;

        if (scrollPositionb > 0) {
            document.documentElement.classList.add('scrolled');
        } else {
            document.documentElement.classList.remove('scrolled');
        }

        if (scrollPosition > 60) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }

        if (window.scrollY > 0) {
            logo.src = 'Logos/logo-transparent-png.png';
        } else {
            logo.src = 'Logos/logo-transparent-wht.png';
        }
    }

    window.addEventListener('scroll', handleScroll);

    if (headerBackground) {
        updateBackgroundImage();
        setInterval(updateBackgroundImage, 5000);
    } else {
        console.log('headerBackground not found');
    }

    if (headerTitle) {
        headerTitle.addEventListener('aos:in', () => {
            headerTitle.classList.add('animate');
        });
    } else {
        console.log('headerTitle not found');
    }

    buyNowButton.addEventListener('click', () => {
        // Add your buy now functionality here
        console.log('Buy Now button clicked!');
    });
});
