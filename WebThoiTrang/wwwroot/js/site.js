// Site-wide JavaScript for Levents Store

$(document).ready(function () {

    localStorage.removeItem('form_aiTryOnForm');
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Smooth scrolling for anchor links
    $('a[href*="#"]:not([href="#"])').click(function () {
        if (location.pathname.replace(/^\//, '') == this.pathname.replace(/^\//, '') && location.hostname == this.hostname) {
            var target = $(this.hash);
            target = target.length ? target : $('[name=' + this.hash.slice(1) + ']');
            if (target.length) {
                $('html, body').animate({
                    scrollTop: target.offset().top - 80
                }, 1000);
                return false;
            }
        }
    });

    // Add to cart functionality
    window.addToCart = function (productId, size, color, quantity) {
        // Show loading state
        const btn = event.target;
        const originalText = btn.innerHTML;
        btn.innerHTML = '<span class="loading"></span> Đang thêm...';
        btn.disabled = true;

        // Simulate API call
        setTimeout(() => {
            // Get existing cart from localStorage
            let cart = JSON.parse(localStorage.getItem('levents_cart') || '[]');

            // Add item to cart
            const existingItem = cart.find(item =>
                item.productId === productId &&
                item.size === size &&
                item.color === color
            );

            if (existingItem) {
                existingItem.quantity += quantity;
            } else {
                cart.push({
                    productId: productId,
                    size: size,
                    color: color,
                    quantity: quantity,
                    addedAt: new Date().toISOString()
                });
            }

            // Save to localStorage
            localStorage.setItem('levents_cart', JSON.stringify(cart));

            // Update cart counter
            updateCartCounter();

            // Show success message
            showNotification('Đã thêm sản phẩm vào giỏ hàng!', 'success');

            // Reset button
            btn.innerHTML = originalText;
            btn.disabled = false;
        }, 1000);
    };

    // Update cart counter
    function updateCartCounter() {
        const cart = JSON.parse(localStorage.getItem('levents_cart') || '[]');
        const totalItems = cart.reduce((sum, item) => sum + item.quantity, 0);

        // Update cart badge
        const cartBadge = document.querySelector('.cart-badge');
        if (cartBadge) {
            cartBadge.textContent = totalItems;
            cartBadge.style.display = totalItems > 0 ? 'inline-block' : 'none';
        }
    }

    // Show notification
    function showNotification(message, type = 'info') {
        // Remove existing notifications
        $('.notification').remove();

        const notification = $(`
            <div class="notification alert alert-${type} alert-dismissible fade show position-fixed" 
                 style="top: 100px; right: 20px; z-index: 9999; min-width: 300px;">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `);

        $('body').append(notification);

        // Auto hide after 3 seconds
        setTimeout(() => {
            notification.alert('close');
        }, 3000);
    }

    // Initialize cart counter on page load
    updateCartCounter();

    // Product image gallery
    window.changeMainImage = function (imageSrc) {
        const mainImage = document.getElementById('mainImage');
        if (mainImage) {
            mainImage.src = imageSrc;

            // Update active thumbnail
            document.querySelectorAll('.thumbnail-image').forEach(img => {
                img.classList.remove('active');
            });
            event.target.classList.add('active');
        }
    };

    // Quantity controls
    window.increaseQuantity = function (inputId = 'quantity') {
        const input = document.getElementById(inputId);
        if (input) {
            input.value = parseInt(input.value) + 1;
            input.dispatchEvent(new Event('change'));
        }
    };

    window.decreaseQuantity = function (inputId = 'quantity') {
        const input = document.getElementById(inputId);
        if (input && parseInt(input.value) > 1) {
            input.value = parseInt(input.value) - 1;
            input.dispatchEvent(new Event('change'));
        }
    };

    // Size selection
    $(document).on('click', '.size-btn', function () {
        $('.size-btn').removeClass('selected');
        $(this).addClass('selected');
    });

    // Color selection
    $(document).on('click', '.color-option', function () {
        $('.color-option').removeClass('selected');
        $(this).addClass('selected');
    });

    // Search functionality
    $('#searchInput').on('input', function () {
        const searchTerm = $(this).val().toLowerCase();
        const products = $('.product-card');

        products.each(function () {
            const productName = $(this).find('.product-name').text().toLowerCase();
            if (productName.includes(searchTerm)) {
                $(this).parent().show();
            } else {
                $(this).parent().hide();
            }
        });
    });

    // Filter products by category
    $('.category-filter').on('click', function (e) {
        //e.preventDefault();
        const category = $(this).data('category');

        $('.category-filter').removeClass('active');
        $(this).addClass('active');

        if (category === 'all') {
            $('.product-card').parent().show();
        } else {
            $('.product-card').parent().hide();
            $(`.product-card[data-category="${category}"]`).parent().show();
        }
    });

    // Price range filter
    $('#priceRange').on('input', function () {
        const maxPrice = parseInt($(this).val());
        $('#priceValue').text(maxPrice.toLocaleString('vi-VN') + ' VND');

        $('.product-card').each(function () {
            const price = parseInt($(this).find('.product-price').text().replace(/[^\d]/g, ''));
            if (price <= maxPrice) {
                $(this).parent().show();
            } else {
                $(this).parent().hide();
            }
        });
    });

    // Wishlist functionality
    window.toggleWishlist = function (productId) {
        let wishlist = JSON.parse(localStorage.getItem('levents_wishlist') || '[]');
        const index = wishlist.indexOf(productId);

        if (index > -1) {
            wishlist.splice(index, 1);
            showNotification('Đã xóa khỏi danh sách yêu thích', 'info');
        } else {
            wishlist.push(productId);
            showNotification('Đã thêm vào danh sách yêu thích', 'success');
        }

        localStorage.setItem('levents_wishlist', JSON.stringify(wishlist));
        updateWishlistUI();
    };

    // Update wishlist UI
    function updateWishlistUI() {
        const wishlist = JSON.parse(localStorage.getItem('levents_wishlist') || '[]');
        $('.wishlist-btn').each(function () {
            const productId = parseInt($(this).data('product-id'));
            if (wishlist.includes(productId)) {
                $(this).addClass('active').find('i').removeClass('far').addClass('fas');
            } else {
                $(this).removeClass('active').find('i').removeClass('fas').addClass('far');
            }
        });
    }

    // Initialize wishlist UI
    updateWishlistUI();

    // Newsletter subscription
    $('#newsletterForm').on('submit', function (e) {
        e.preventDefault();
        const email = $(this).find('input[type="email"]').val();

        // Simulate API call
        $(this).find('button').html('<span class="loading"></span>').prop('disabled', true);

        setTimeout(() => {
            showNotification('Đăng ký thành công! Cảm ơn bạn đã quan tâm.', 'success');
            $(this).find('input[type="email"]').val('');
            $(this).find('button').html('Đăng ký').prop('disabled', false);
        }, 1500);
    });

    // Lazy loading for images
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                    imageObserver.unobserve(img);
                }
            });
        });

        document.querySelectorAll('img[data-src]').forEach(img => {
            imageObserver.observe(img);
        });
    }

    // Back to top button
    const backToTopBtn = $('<button id="backToTop" class="btn btn-primary position-fixed" style="bottom: 20px; right: 20px; z-index: 1000; display: none;"><i class="fas fa-chevron-up"></i></button>');
    $('body').append(backToTopBtn);

    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('#backToTop').fadeIn();
        } else {
            $('#backToTop').fadeOut();
        }
    });

    $('#backToTop').click(function () {
        $('html, body').animate({ scrollTop: 0 }, 600);
    });

    // Product comparison
    window.addToCompare = function (productId) {
        let compare = JSON.parse(localStorage.getItem('levents_compare') || '[]');

        if (compare.length >= 3) {
            showNotification('Chỉ có thể so sánh tối đa 3 sản phẩm', 'warning');
            return;
        }

        if (compare.includes(productId)) {
            showNotification('Sản phẩm đã có trong danh sách so sánh', 'info');
            return;
        }

        compare.push(productId);
        localStorage.setItem('levents_compare', JSON.stringify(compare));
        showNotification('Đã thêm vào danh sách so sánh', 'success');
        updateCompareCounter();
    };

    function updateCompareCounter() {
        const compare = JSON.parse(localStorage.getItem('levents_compare') || '[]');
        const counter = document.querySelector('.compare-counter');
        if (counter) {
            counter.textContent = compare.length;
            counter.style.display = compare.length > 0 ? 'inline-block' : 'none';
        }
    }

    // Initialize compare counter
    updateCompareCounter();

    // Form validation
    $('form').on('submit', function (e) {
        const form = this;
        let isValid = true;

        // Remove previous error messages
        $(form).find('.error-message').remove();
        $(form).find('.is-invalid').removeClass('is-invalid');

        // Validate required fields
        $(form).find('[required]').each(function () {
            const field = $(this);
            const value = field.val().trim();

            if (!value) {
                isValid = false;
                field.addClass('is-invalid');
                field.after('<div class="error-message text-danger small mt-1">Trường này là bắt buộc</div>');
            }
        });

        // Validate email fields
        $(form).find('input[type="email"]').each(function () {
            const field = $(this);
            const email = field.val().trim();
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

            if (email && !emailRegex.test(email)) {
                isValid = false;
                field.addClass('is-invalid');
                field.after('<div class="error-message text-danger small mt-1">Email không hợp lệ</div>');
            }
        });

        // Validate phone fields
        $(form).find('input[type="tel"]').each(function () {
            const field = $(this);
            const phone = field.val().trim();
            const phoneRegex = /^[0-9]{10,11}$/;

            if (phone && !phoneRegex.test(phone)) {
                isValid = false;
                field.addClass('is-invalid');
                field.after('<div class="error-message text-danger small mt-1">Số điện thoại không hợp lệ</div>');
            }
        });

        if (!isValid) {
            e.preventDefault();
            // Scroll to first error
            const firstError = $(form).find('.is-invalid').first();
            if (firstError.length) {
                $('html, body').animate({
                    scrollTop: firstError.offset().top - 100
                }, 500);
            }
        }
    });
    // Auto-save form data
    $('form input, form select, form textarea').on('change', function () {
        const form = $(this).closest('form');
        const formId = form.attr('id') || 'default';

        // Bỏ qua hoàn toàn form AI này
        if (formId === 'aiTryOnForm') {
            return;
        }

        const formData = form.serializeArray();
        if (formData.length > 0) {
            localStorage.setItem(`form_${formId}`, JSON.stringify(formData));
        } else {
            localStorage.removeItem(`form_${formId}`);
        }
    });

    // Restore form data
    $('form').each(function () {
        const form = $(this);
        const formId = form.attr('id') || 'default';

        // Bỏ qua hoàn toàn form AI này
        if (formId === 'aiTryOnForm') {
            return;
        }

        const savedData = localStorage.getItem(`form_${formId}`);
        if (savedData) {
            const formData = JSON.parse(savedData);
            formData.forEach(field => {
                const input = form.find(`[name="${field.name}"]`);
                if (input.length) {
                    if (input.is(':radio') || input.is(':checkbox')) {
                        input.prop('checked', input.val() === field.value);
                    } else {
                        input.val(field.value);
                    }
                }
            });
        }
    });
});

// Global utility functions
window.LeventsStore = {
    // Format currency
    formatCurrency: function (amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    },

    // Format number
    formatNumber: function (number) {
        return new Intl.NumberFormat('vi-VN').format(number);
    },

    // Get cart items
    getCartItems: function () {
        return JSON.parse(localStorage.getItem('levents_cart') || '[]');
    },

    // Clear cart
    clearCart: function () {
        localStorage.removeItem('levents_cart');
        updateCartCounter();
    },

    // Get wishlist items
    getWishlistItems: function () {
        return JSON.parse(localStorage.getItem('levents_wishlist') || '[]');
    },

    // Show loading overlay
    showLoading: function () {
        const overlay = $(`
            <div id="loadingOverlay" class="position-fixed w-100 h-100 d-flex align-items-center justify-content-center" 
                 style="top: 0; left: 0; background: rgba(255, 255, 255, 0.8); z-index: 9999;">
                <div class="text-center">
                    <div class="loading mb-3" style="width: 40px; height: 40px; border-width: 4px;"></div>
                    <p>Đang xử lý...</p>
                </div>
            </div>
        `);
        $('body').append(overlay);
    },

    // Hide loading overlay
    hideLoading: function () {
        $('#loadingOverlay').remove();
    },

    // API helper
    api: {
        baseUrl: '/api',

        get: function (endpoint) {
            return fetch(`${this.baseUrl}${endpoint}`)
                .then(response => response.json());
        },

        post: function (endpoint, data) {
            return fetch(`${this.baseUrl}${endpoint}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data)
            }).then(response => response.json());
        }
    }
};